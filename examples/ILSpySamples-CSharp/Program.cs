


// See https://aka.ms/new-console-template for more information
Console.WriteLine(Thingy.DoThingAsync().GetAwaiter().GetResult());



public class Thingy
{
    public static async Task<int> DoThingAsync()
    {
        await Task.Yield();
        return 42;
    }
}