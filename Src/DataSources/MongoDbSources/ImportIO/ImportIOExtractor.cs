using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace FalconSoft.ReactiveWorksheets.MongoDbSources.ImportIO
{
    public delegate void QueryHandler(Query query, Dictionary<string, object> data);

    public class Query
    {
        private int _jobsCompleted;
        private int _jobsStarted;
        private int _jobsSpawned;
        private bool _finished;
        public bool IsFinished { get { return _finished; } set { _finished = value; } }
        public QueryHandler QueryCallback;
        Dictionary<string, object> _queryInput;

        public Query(Dictionary<string, object> queryInput, QueryHandler queryCallback)
        {
            _queryInput = queryInput;
            QueryCallback = queryCallback;
        }

        public void OnMessage(Dictionary<string, object> data)
        {
            var messageType = (string)data["type"];
            switch (messageType)
            {
                case "SPAWN":
                    _jobsSpawned++;
                    break;
                case "INIT":
                case "START":
                    _jobsStarted++;
                    break;
                case "STOP":
                    _jobsCompleted++;
                    break;
            }
            _finished = _jobsStarted == _jobsCompleted && _jobsSpawned + 1 == _jobsStarted && _jobsStarted > 0 || (messageType.Equals("ERROR") || messageType.Equals("UNAUTH") || messageType.Equals("CANCEL"));
            QueryCallback(this, data);
        }
    }
    public class ImportIOExtractor
    {
        private Guid _userGuid;
        private readonly string _apiKey;
        private const string MessagingChannel = "/messaging";
        private readonly string _url;
        private int _msgId;
        private string _clientId;
        private bool _isConnected;
        public Action<string> ErrorCame;
        private string _error;
        public string Error
        {
            get { return _error; }
            private set
            {
                if (_error == value) return;
                _error = value;
                if (ErrorCame != null)
                    ErrorCame(_error);
            }
        }
        readonly CookieContainer _cookieContainer = new CookieContainer();
        readonly Dictionary<Guid, Query> _queries = new Dictionary<Guid, Query>();
        private readonly BlockingCollection<Dictionary<string, object>> _messageQueue = new BlockingCollection<Dictionary<string, object>>();

        public ImportIOExtractor(string host = "http://query.import.io", Guid userGuid = default(Guid), string apiKey = null, Action<string> onError = null)
        {
            _userGuid = userGuid;
            _apiKey = apiKey;
            _url = host + "/query/comet/";
            _clientId = null;
            ErrorCame = onError;
        }

        public void Login(string username, string password, string host = "http://api.import.io")
        {
            var loginParams = "username=" + HttpUtility.UrlEncode(username) + "&password=" + HttpUtility.UrlEncode(password);
            var searchUrl = host + "/auth/login";
            var loginRequest = (HttpWebRequest)WebRequest.Create(searchUrl);

            loginRequest.Method = "POST";
            loginRequest.ContentType = "application/x-www-form-urlencoded";
            loginRequest.ContentLength = loginParams.Length;

            loginRequest.CookieContainer = _cookieContainer;

            using (var dataStream = loginRequest.GetRequestStream())
            {
                dataStream.Write(Encoding.UTF8.GetBytes(loginParams), 0, loginParams.Length);

                var loginResponse = (HttpWebResponse)loginRequest.GetResponse();


                if (loginResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("Could not log in, code:" + loginResponse.StatusCode);
                }
                foreach (Cookie cookie in loginResponse.Cookies)
                {
                    if (cookie.Name.Equals("AUTH"))
                    {
                        Console.WriteLine(@"Login Successful");
                    }
                }
            }
        }

        public List<Dictionary<string, object>> Request(string channel, Dictionary<string, object> data = null, string path = "", bool doThrow = true)
        {
            var dataPacket = new Dictionary<string, object>
            {
                {"channel", channel},
                {"connectionType", "long-polling"},
                {"id", (_msgId++).ToString(CultureInfo.InvariantCulture)}
            };

            if (_clientId != null) dataPacket.Add("clientId", _clientId);
            if (data != null)
            {
                foreach (var entry in data)
                {
                    dataPacket.Add(entry.Key, entry.Value);
                }
            }
            var url = _url + path;
            if (_apiKey != null) url += "?_user=" + HttpUtility.UrlEncode(_userGuid.ToString()) + "&_apikey=" + HttpUtility.UrlEncode(_apiKey);

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip");
            var dataJson = JsonConvert.SerializeObject(new List<object> { dataPacket });
            request.ContentLength = dataJson.Length;
            request.CookieContainer = _cookieContainer;
            try
            {
                using (var dataStream = request.GetRequestStream())
                {
                    dataStream.Write(Encoding.UTF8.GetBytes(dataJson), 0, dataJson.Length);
                    try
                    {
                        var response = (HttpWebResponse)request.GetResponse();
                        using (var responseStream = new StreamReader(response.GetResponseStream()))
                        {
                            var responseJson = responseStream.ReadToEnd();
                            var responseList =
                                JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(responseJson);
                            foreach (var responseDict in responseList)
                            {
                                if (responseDict.ContainsKey("successful") && (bool)responseDict["successful"] != true)
                                {
                                    if (doThrow) throw new Exception("Unsucessful request");
                                }
                                if (!responseDict["channel"].Equals(MessagingChannel)) continue;
                                if (responseDict.ContainsKey("data"))
                                {
                                    _messageQueue.Add(((Newtonsoft.Json.Linq.JObject)responseDict["data"]).ToObject<Dictionary<string, object>>());
                                }
                            }
                            return responseList;
                        }
                    }
                    catch (Exception e)
                    {
                        Error = e.Message;
                        return new List<Dictionary<string, object>>();
                    }
                }
            }
            catch (WebException e)
            {
                Error = e.Message;
                return new List<Dictionary<string, object>>();
            }
        }

        public bool Handshake()
        {
            var handshakeData = new Dictionary<string, object>
            {
                {"version", "1.0"},
                {"minimumVersion", "0.9"},
                {"supportedConnectionTypes", new List<string> {"long-polling"}},
                {"advice", new Dictionary<string, int> {{"timeout", 60000}, {"interval", 0}}}
            };
            var responseList = Request("/meta/handshake", handshakeData, "handshake");
            if (responseList.Count == 0) return false;
            _clientId = (string)responseList[0]["clientId"];
            return true;
        }

        public bool Connect()
        {
            if (_isConnected)
            {
                Error = "Already Connected";
                return false;
            }
            if (!Handshake()) return false;
            var subscribeData = new Dictionary<string, object> { { "subscription", MessagingChannel } };
            Request("/meta/subscribe", subscribeData);
            _isConnected = true;
            (new Thread(Poll) { IsBackground = true }).Start();
            (new Thread(PollQueue) { IsBackground = true }).Start();
            return true;
        }

        public void Disconnect()
        {
            Request("/meta/disconnect");
            _isConnected = false;
        }

        private void Poll()
        {
            while (_isConnected)
            {
                if (string.IsNullOrEmpty(Error))
                    Request("/meta/connect", null, "connect", false);
            }
        }

        private void PollQueue()
        {
            while (_isConnected)
            {
                if (string.IsNullOrEmpty(Error))
                    ProcessMessage(_messageQueue.Take());
            }
        }

        private void ProcessMessage(Dictionary<string, object> data)
        {
            var requestId = Guid.Parse((string)data["requestId"]);
            var query = _queries[requestId];
            query.OnMessage(data);
            if (query.IsFinished) _queries.Remove(requestId);
        }

        public void DoQuery(Dictionary<string, object> query, QueryHandler queryHandler)
        {
            var requestId = Guid.NewGuid();
            _queries.Add(requestId, new Query(query, queryHandler));
            query.Add("requestId", requestId);
            Request("/service/query", new Dictionary<string, object> { { "data", query } });
        }
    }
}
