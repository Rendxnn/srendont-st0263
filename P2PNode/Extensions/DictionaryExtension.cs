namespace P2PNode.Extensions
{
    public static class DictionaryExtension
    {
        public static FingerTableMessage ConvertToFTMessage(this Dictionary<string, string> dict, int SenderId)
        {
            return new FingerTableMessage
            {
                SenderId = SenderId,
                Pairs = { dict.Select(kv => new FingerTablePair { Key = kv.Key, Value = kv.Value }) }
            };
        }

        public static bool AddToFT(this Dictionary<string, string> dict, int newId, string newAddress)
        {
            int min = int.MaxValue;
            int max = 0;
            foreach (KeyValuePair<string, string> pair in dict)
            {
                string range = pair.Key;
                string address = pair.Value;

                int[] rangeValues = range.Split(",").Select(x => int.Parse(x)).ToArray();

                if (rangeValues[0] < min) { min = rangeValues[0]; }

                if (rangeValues[1] < max) { max = rangeValues[1]; }

                if (newId > rangeValues[0] && (newId < rangeValues[1] || rangeValues[1] == -1))
                {
                    dict.Remove(pair.Key);

                    dict.Add($"{rangeValues[0]},{newId}", address);
                    dict.Add($"{newId},{rangeValues[1]}", address);

                    return true;
                }
            }

            if (newId < min)
            {
                dict.Add($"{newId},{min}", newAddress);
                return true;
            }


            return false;
        }
    }
}
