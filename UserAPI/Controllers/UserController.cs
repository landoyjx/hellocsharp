using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UserAPI.Models;
using HelloWorld.Interfaces;
using Orleans;
using Orleans.Runtime;
using System;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;

namespace UserAPI.Controllers
{
    [ApiController]
    public class UserController : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        private readonly UserContext _context;
        public UserController(UserContext context)
        {
            _context = context;
        }

        private static Func<Exception, Task<bool>> CreateRetryFilter(int maxAttempts = 5)
        {
            var attempt = 0;
            return RetryFilter;

            async Task<bool> RetryFilter(Exception exception)
            {
                attempt++;
                Console.WriteLine($"Cluster client attempt {attempt} of {maxAttempts} failed to connect to cluster.  Exception: {exception}");
                if (attempt > maxAttempts)
                {
                    return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(4));
                return true;
            }
        }

        private bool IsEmpty(string value)
        {
            if (value == null || value == "") return true;
            else return false;
        }

        private async Task<bool> RequestOk(HttpRequest request)
        {
            string ip = request.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            Console.WriteLine("Comming IP " + ip);

            // connect silo
            var client = new ClientBuilder()
                    .UseLocalhostClustering()
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "HelloWorldApp";
                    })
                    .ConfigureLogging(logging => logging.AddConsole())
                    .Build();

            await client.Connect(CreateRetryFilter());
            Console.WriteLine("Client successfully connect to silo host");

            var grain = client.GetGrain<ILimit>(ip);
            var ret = await grain.checkLimitOk();

            // add to black list
            if (!ret)
            {
                BlackItem item = new BlackItem();
                item.Ip = ip;
                item.LastTime = DateTime.Now;
                _context.BlackLists.Remove(item);
                _context.BlackLists.Add(item);
                await _context.SaveChangesAsync();
            }

            return ret;
        }

        // POST: api/register
        [Microsoft.AspNetCore.Mvc.HttpPost]
        [Microsoft.AspNetCore.Mvc.Route("api/register")]
        public async Task<ActionResult<string>> PostRegister(RegisterItem item)
        {
            // TODO, should use filter
            bool limit = await RequestOk(Request);
            if (!limit)
            {
                return new StatusCodeResult(429);
            }

            string username = item.username;
            if (IsEmpty(username))
            {
                return "invalid username";
            }
            string password = item.password;
            if (IsEmpty(password))
            {
                return "invalid password";
            }
            string name = item.name != null ? item.name : "";

            // check exists
            var user = await _context.Users.FindAsync(username.ToLower());
            if (user != null)
            {
                return "user exists";
            }

            // save
            user = new User();
            user.Username = username;
            user.Name = name;
            user.Uid = "uid-" + System.Guid.NewGuid().ToString();
            // TODO, password should replace by hashed password
            // should not save plain password
            user.Password = password;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return "success";
        }

        // POST: api/login
        [Microsoft.AspNetCore.Mvc.HttpPost]
        [Microsoft.AspNetCore.Mvc.Route("api/login")]
        public async Task<ActionResult<string>> PostLogin(LoginItem item)
        {
            // TODO, should use filter
            bool limit = await RequestOk(Request);
            if (!limit)
            {
                return new StatusCodeResult(429);
            }

            string username = item.username;
            if (IsEmpty(username))
            {
                return "invalid username";
            }
            string password = item.password;
            if (IsEmpty(password))
            {
                return "invalid password";
            }

            // connect silo
            var client = new ClientBuilder()
                    .UseLocalhostClustering()
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "HelloWorldApp";
                    })
                    .ConfigureLogging(logging => logging.AddConsole())
                    .Build();

            await client.Connect(CreateRetryFilter());
            Console.WriteLine("Client successfully connect to silo host");

            string _username = username.ToLower();
            var grain = client.GetGrain<IValue>(_username);
            var ret = await grain.GetAsync();
            if (!IsEmpty(ret))
            {
                return ret;
            }

            var user = await _context.Users.FindAsync(_username);
            if (user == null)
            {
                return "user not exist";
            }

            // TODO, should compare hashed password
            if (user.Password != password)
            {
                return "invalid password";
            }

            string jwt = System.Guid.NewGuid().ToString();
            grain.SetAsync(jwt);
            // reverse
            var jwtGrain = client.GetGrain<IValue>(jwt);
            jwtGrain.SetAsync(_username);

            return jwt;
        }

        // GET: api/info
        [Microsoft.AspNetCore.Mvc.HttpGet]
        [Microsoft.AspNetCore.Mvc.Route("api/info")]
        public async Task<ActionResult<UserInfo>> GetInfo()
        {
            // TODO, should use filter
            bool limit = await RequestOk(Request);
            if (!limit)
            {
                return new StatusCodeResult(429);
            }

            StringValues oo;
            Request.Headers.TryGetValue("Authorization", out oo);
            string head = oo.ToString();
            string jwt = head.Substring("Bearer ".Length);
            if (IsEmpty(jwt))
            {
                return new UnauthorizedResult();
            }

            // connect silo
            var client = new ClientBuilder()
                    .UseLocalhostClustering()
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "HelloWorldApp";
                    })
                    .ConfigureLogging(logging => logging.AddConsole())
                    .Build();

            await client.Connect(CreateRetryFilter());
            Console.WriteLine("Client successfully connect to silo host");

            var grain = client.GetGrain<IValue>(jwt);
            string username = await grain.GetAsync();
            if (IsEmpty(username))
            {
                return new UnauthorizedResult();
            }

            var user = await _context.Users.FindAsync(username);
            UserInfo info = new UserInfo();
            info.userId = user.Uid;
            info.name = user.Name;

            return new JsonResult(info);
        }
    }
}
