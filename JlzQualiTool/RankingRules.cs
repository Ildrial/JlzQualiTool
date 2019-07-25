namespace JlzQualiTool
{
    public interface IRankingRules
    {
    }

    public class NoRules : IRankingRules
    {
        public static IRankingRules Get = new NoRules();
    }
}