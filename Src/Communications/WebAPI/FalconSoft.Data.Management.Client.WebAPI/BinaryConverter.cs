using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace FalconSoft.Data.Management.Client.WebAPI
{
    public class BinaryConverter
    {
        private readonly object Lock = new object();

        public byte[] CastToBytes(object obj)
        {
            lock (Lock)
            {
                if (obj == null)
                    return null;

                var bf = new BinaryFormatter();
                using (var ms = new MemoryStream())
                {
                    bf.Serialize(ms, obj);

                    return ms.ToArray();
                }
            }
        }

        public T CastTo<T>(byte[] byteArray)
        {
            lock (Lock)
            {
                if (!byteArray.Any())
                    return default(T);

                using (var memStream = new MemoryStream())
                {
                    var binForm = new BinaryFormatter();
                    memStream.Write(byteArray, 0, byteArray.Length);
                    memStream.Seek(0, SeekOrigin.Begin);
                    return (T)binForm.Deserialize(memStream);
                }
            }
        }

        public byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}