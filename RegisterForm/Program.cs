using System;
using System.Net;
using RegisterForm.Controllers;
using RegisterForm.Utilities;

class Program
{
    static AccountController accountController;

    static async Task Main(string[] args)
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5000/");
        listener.Start();
        Console.WriteLine("Server running on http://localhost:5000/");

        var router = new Router();

        while (true)
        {
            try
            {
                var context = await listener.GetContextAsync();
                await router.RouteAsync(context);
            }
            catch (HttpListenerException hlex)
            {
                Console.WriteLine($"Listener exception: {hlex.Message}");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected exception: {ex}");
            }
        }

        listener.Stop();
        listener.Close();


    }

}
