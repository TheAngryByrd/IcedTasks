


// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");



public class Thingy
{
    public async Task<int> DoThingAsync()
    {
        await Task.Yield();
        return 42;
    }
}