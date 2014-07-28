using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    internal static class BinaryConverter
    {
        private static readonly object Lock = new object();

        public static byte[] CastToBytes(object obj)
        {
            lock (Lock)
            {
                if (obj == null)
                    return null;

                var bf = new BinaryFormatter();
                var ms = new MemoryStream();
                bf.Serialize(ms, obj);

                return ms.ToArray();
            }
        }

        public static T CastTo<T>(byte[] byteArray)
        {
            lock (Lock)
            {
                if (!byteArray.Any())
                    return default(T);

                var memStream = new MemoryStream();
                var binForm = new BinaryFormatter();
                memStream.Write(byteArray, 0, byteArray.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                return (T)binForm.Deserialize(memStream);
            }
        }
    }
}
