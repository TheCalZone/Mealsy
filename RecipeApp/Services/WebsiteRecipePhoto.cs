using System.Net;
using System.Net.Sockets;

namespace RecipeApp.Services;

/// <summary>Downloads a bounded public HTTPS image, then saves a small metadata-free copy.</summary>
public static class WebsiteRecipePhoto
{
    public static async Task<string> Load(string source)
    {
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var handler=new SocketsHttpHandler {
            AllowAutoRedirect=false,UseCookies=false,
            ConnectCallback=async(context,token)=>{
                var addresses=await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host,token);
                var address=addresses.FirstOrDefault(IsPublic)??throw new InvalidOperationException("This photo is not publicly hosted.");
                var socket=new Socket(address.AddressFamily,SocketType.Stream,ProtocolType.Tcp);
                try{await socket.ConnectAsync(new IPEndPoint(address,context.DnsEndPoint.Port),token);return new NetworkStream(socket,true);}
                catch{socket.Dispose();throw;}
            }
        };
        using var client=new HttpClient(handler);
        var uri=new Uri(source);
        for(var redirects=0;redirects<4;redirects++)
        {
            if(!WebsiteRecipe.IsPhotoUrl(uri))throw new InvalidOperationException("This photo link is unsupported.");
            using var response=await client.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead,timeout.Token);
            if((int)response.StatusCode is >=300 and <400){uri=new Uri(uri,response.Headers.Location??throw new InvalidOperationException("Missing photo redirect."));continue;}
            response.EnsureSuccessStatusCode();
            if(response.Content.Headers.ContentLength>15*1024*1024||response.Content.Headers.ContentType?.MediaType?.StartsWith("image/",StringComparison.OrdinalIgnoreCase)!=true)
                throw new InvalidOperationException("This photo could not be imported.");
            using var stream=await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer=new MemoryStream();var bytes=new byte[81920];int read;
            while((read=await stream.ReadAsync(bytes,timeout.Token))>0){if(buffer.Length+read>15*1024*1024)throw new InvalidOperationException("Photo too large.");await buffer.WriteAsync(bytes.AsMemory(0,read),timeout.Token);}
            buffer.Position=0;return await FeedPhoto.PrepareAsync(buffer);
        }
        throw new InvalidOperationException("Too many photo redirects.");
    }
    private static bool IsPublic(IPAddress address)
    {
        if(address.IsIPv4MappedToIPv6)address=address.MapToIPv4();
        if(IPAddress.IsLoopback(address))return false;
        var b=address.GetAddressBytes();
        if(address.AddressFamily==AddressFamily.InterNetwork)
            return b[0]!=0&&b[0]!=10&&b[0]!=127&&b[0]<224&&!(b[0]==169&&b[1]==254)&&!(b[0]==172&&b[1]>=16&&b[1]<=31)&&!(b[0]==192&&b[1]==168)&&!(b[0]==100&&b[1]>=64&&b[1]<=127);
        return b[0]>=0x20&&b[0]<=0x3f; // Global IPv6 unicast only.
    }
}
