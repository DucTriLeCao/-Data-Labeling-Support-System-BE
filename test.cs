using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        var client = new HttpClient();
        // first create a user just to get a token, but we might just use the database
        // let's just create an admin in the database if not exists
    }
}
