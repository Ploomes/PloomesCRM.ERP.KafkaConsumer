using System.Reflection.Metadata;

namespace PloomesCRM.ERP.KafkaConsumer.Business
{
    public static class ConsumerConstParameters
    {
        public static readonly int MessageConsumeOnRound = 150;

        public static int MessagesLimitOnMemory = 3 * MessageConsumeOnRound;

        public static int ObjToBeSentLaterLimit = MessageConsumeOnRound/2;
    }
}
