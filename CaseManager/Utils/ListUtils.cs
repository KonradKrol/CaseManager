namespace CaseManager.Utils;

public static class ListUtils
{
    extension<T>(List<T> list)
    {
        public T GetRandomElement()
        {
            if (list.Count == 0)
            {
                throw new InvalidOperationException("The list is empty.");
            }
            var random = new Random();
            return list[random.Next(list.Count)];
        }
    }
}