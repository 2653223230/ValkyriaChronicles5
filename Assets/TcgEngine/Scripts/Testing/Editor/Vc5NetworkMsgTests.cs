#if UNITY_EDITOR
using NUnit.Framework;

namespace TcgEngine.Testing.Editor
{
[Category("VC5DemoGate")]
public class Vc5NetworkMsgTests
    {
        [Test]
        public void MsgString_RoundTripsText()
        {
            MsgString msg = new MsgString { text = "card-uid-123" };

            byte[] bytes = NetworkTool.NetSerialize(msg);
            MsgString result = NetworkTool.NetDeserialize<MsgString>(bytes);

            Assert.NotNull(result);
            Assert.AreEqual("card-uid-123", result.text);
        }
    }
}
#endif
