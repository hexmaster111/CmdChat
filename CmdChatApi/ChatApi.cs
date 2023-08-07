using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using CmdChatApi;
using CmdChatApi.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CmdChatApi
{
    internal class ApiInternal : NotifyObject
    {
        public string ServerAddress { get; init; }
        public ulong UserId { get; init; }

        private string GetSync(string url)
        {
            var uri = new Uri(url);
            using WebClient wc = new WebClient();
            return wc.DownloadString(uri);
        }

        private Task<T> GetResponse<T>(string method, param[]? parameters = null)
        {
            StringBuilder sb = new StringBuilder(method);
            if (parameters != null)
            {
                sb.Append('?');
                foreach (param p in parameters)
                {
                    sb.Append(p.Name);
                    sb.Append('=');
                    sb.Append(p.Value);
                    sb.Append('&');
                }

                sb.Remove(sb.Length - 1, 1);
            }

            string response = GetSync(sb.ToString());
            Debug.WriteLine(response);
            return Task.FromResult(JsonConvert.DeserializeObject<T>(response));
        }

        private record param(string Name, string Value);

        //https://localhost:7177/api/Chat/GetSelf?id=5254415346834758247
        private async Task<__User> GetSelf()
        {
            return await GetResponse<__User>(ServerAddress + "/api/Chat/GetSelf", new[] { new param("id", UserId.ToString()) });
        }

        // https://localhost:7177/api/Chat/GetFrends?id=1234
        private async Task<IEnumerable<__User>> GetFriends()
        {
            return await GetResponse<IEnumerable<__User>>(ServerAddress + "/api/Chat/GetFrends", new[] { new param("id", UserId.ToString()) });
        }

        // https://localhost:7177/api/Chat/GetOpenDms?id=1234
        private async Task<IEnumerable<__User>> GetOpenDms() => await GetResponse<IEnumerable<__User>>(ServerAddress + "/api/Chat/GetOpenDms",
            new[]
            {
                new param("id", UserId.ToString())
            });

        //https://localhost:7177/api/Chat/GetDm?id=1234&friendId=4321&endOffset=0&count=20
        private async Task<IEnumerable<__Message>> GetDm(ulong friendId, int endOffset, int count) => await GetResponse<IEnumerable<__Message>>(ServerAddress + "/api/Chat/GetDm",
            new[]
            {
                new param("id", UserId.ToString()),
                new param("friendId", friendId.ToString()),
                new param("endOffset", endOffset.ToString()),
                new param("count", count.ToString())
            });


        private Task<T> PostResponse<T>(string method, param[]? parameters = null, bool isJson = false, string json = "")
        {
            if (!isJson)
            {
                StringBuilder sb = new StringBuilder(method);
                if (parameters != null)
                {
                    sb.Append('?');
                    foreach (param p in parameters)
                    {
                        sb.Append(p.Name);
                        sb.Append('=');
                        sb.Append(p.Value);
                        sb.Append('&');
                    }

                    sb.Remove(sb.Length - 1, 1);
                }

                method += sb.ToString();
            }

            var response = PostSync(method, json);

            Debug.WriteLine(response);
            return Task.FromResult(JsonConvert.DeserializeObject<T>(response));
        }

        private string? PostSync(string url, string data)
        {
            var uri = new Uri(url);
            using WebClient wc = new WebClient()
            {
                Headers =
                {
                    ["Content-Type"] = "application/json"
                },
            };
            return wc.UploadString(uri, "POST", data);
        }

        //POST
        // https://localhost:7177/api/Chat/AddFriend?userId=1234&friendId=4321
        internal async Task AddFriend(ulong friendId) => await PostResponse<object>(ServerAddress + "/api/Chat/AddFriend",
            new[]
            {
                new param("userId", UserId.ToString()),
                new param("friendId", friendId.ToString())
            });


        // https://localhost:7177/api/Chat/RemoveFriend?userId=1234&friendId=4321
        internal async Task RemoveFriend(ulong friendId) => await PostResponse<object>(ServerAddress + "/api/Chat/RemoveFriend",
            new[]
            {
                new param("userId", UserId.ToString()),
                new param("friendId", friendId.ToString())
            });

        // https://localhost:7177/api/Chat/SendDm
        //     curl -X 'POST' \
        //     'https://localhost:7177/api/Chat/SendDm' \
        //     -H 'accept: text/plain' \
        //     -H 'Content-Type: application/json' \
        //     -d '{
        //     "fromUser": "1234",
        //     "content": "Message",
        //     "toUser": "4321"
        // }'
        internal async Task SendDm(ulong from, ulong to, string message) => await PostResponse<object>(ServerAddress + "/api/Chat/SendDm", null, true,
            JObject.FromObject(new __ApiMessage(from.ToString(), message, to.ToString())).ToString()
        );

        private record __ApiMessage(string FromUser, string Content, string ToUser);

        public Task StartUpdatesTask(CancellationToken token)
        {
            void UpdateThreadTask()
            {
                while (!token.IsCancellationRequested)
                {
                    var self = GetSelf();
                    var friends = GetFriends();
                    var dms = GetOpenDms();

                    Task.WaitAll(new Task[] { self, friends, dms }, cancellationToken: token);

                    var contacts = friends.Result.Select(friend => new ChatApiContact(friend.Id, friend.Name, friend.Status.ToInternalStatus(), true)).Cast<Contact>().ToDictionary(x => x.UserId);
                    //Add self
                    contacts.Add(self.Result.Id, new ChatApiContact(self.Result.Id, self.Result.Name, self.Result.Status.ToInternalStatus(), true));

                    //if a contact dose not have dms, preload the first 20
                    foreach (var contact in Contacts)
                    {
                        if (contact.DirectMessages.Count == 0)
                        {
                            
                            var d = GetDm(contact.UserId, 0, 20);
                            Task.WaitAll(new Task[] { d }, cancellationToken: token);
                            foreach (var message in d.Result)
                            {
                                //TODO: Bug server needs a concept of current message?
                                contact.DirectMessages.Add(new ChatApiMessage(message.Content, message.Sent, contacts[message.FromUser]));
                            }
                        }
                    }

                    //Update the selected dm
                    {
                        if (SelectedUserId != 0)
                        {
                            var d = GetDm(SelectedUserId, 0, 20);
                            Task.WaitAll(new Task[] { d }, cancellationToken: token);
                            foreach (var message in d.Result)
                            {
                                Contacts.First(x => x.UserId == SelectedUserId).DirectMessages.Add(new ChatApiMessage(message.Content, message.Sent, contacts[message.FromUser]));
                            }
                        }
                    }

                    //remove self
                    contacts.Remove(self.Result.Id);

                    // Add friends
                    foreach (var contact in contacts.Where(contact => !Contacts.Contains(contact.Value)))
                    {
                        Contacts.Add(contact.Value);
                    }

                    //Remove friends
                    foreach (var contact in Contacts)
                    {
                        if (!contacts.ContainsKey(contact.UserId))
                        {
                            Contacts.Remove(contact);
                        }
                    }

                    Task.Delay(1000, token).Wait(token);
                }
            }

            return Task.Run(UpdateThreadTask, token);
        }


        public ObservableCollection<Contact> Contacts { get; init; }
        public ulong SelectedUserId { get; set; }
    }


    public class ChatApiContact : Contact
    {
        public ChatApiContact(ulong userId, string name, ContactStatus status, bool isFriend)
        {
            UserId = userId;
            Name = name;
            Status = status;
            IsFriend = isFriend;
        }
    }

    public class ChatApiMessage : Message
    {
        public ChatApiMessage(string messageText, DateTime timestamp, Contact sender) : base(messageText, timestamp, sender)
        {
        }
    }

    public class ChatApi
    {
        public string ServerAddress { get; }
        public ulong UserId { get; }
        public ObservableCollection<Contact> Contacts { get; } = new();

        private ApiInternal ApiInternal { get; }
        private CancellationTokenSource CancellationTokenSource { get; } = new();

        public ulong SelectedUserId
        {
            get => ApiInternal.SelectedUserId;
            set => ApiInternal.SelectedUserId = value;
        }

        private Task _updatesTask;
        private Contact _selectedContact;

        public ChatApi(string serverAddress, ulong userId)
        {
            ServerAddress = serverAddress;
            UserId = userId;
            ApiInternal = new ApiInternal
            {
                ServerAddress = serverAddress,
                UserId = userId,
                Contacts = Contacts,
            };
        }

        public void StartUpdates()
        {
            _updatesTask = ApiInternal.StartUpdatesTask(CancellationTokenSource.Token);
        }

        public void StopUpdates()
        {
            CancellationTokenSource.Cancel();
            _updatesTask.Wait();
        }

        public void AddFriend(ulong id)
        {
            ApiInternal.AddFriend(id).Wait();
        }

        public void RemoveFriend(ulong id)
        {
            ApiInternal.RemoveFriend(id).Wait();
        }

        public void SendDm(ulong toId, string message)
        {
            ApiInternal.SendDm(UserId, toId, message).Wait();
        }
    }

    internal record __Message(ulong FromUser, string Content, DateTime Sent);

    internal record __ErrorT(string? Error, bool Success);

    internal enum __OnLineStatus
    {
        Online,
        Offline,
        Away,
        Busy,
        DoNotDisturb,
        Invisible,
        Unknown
    }

    [JsonConverter(typeof(UserJsonConverter))]
    internal record __User(string Name, ulong Id, __OnLineStatus Status);


    internal class UserJsonConverter : JsonConverter<__User>
    {
        public override __User ReadJson(JsonReader reader, Type objectType, __User existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            var name = jo["name"].Value<string>();
            var userId = jo["userId"].Value<ulong>();
            var statusI = jo["status"].Value<int>();
            var statusE = (__OnLineStatus)statusI;
            return new __User(name, userId, statusE);
        }

        public override void WriteJson(JsonWriter writer, __User value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

internal static class Ext
{
    public static Contact ToContact(this __User user) => new ChatApiContact(user.Id, user.Name, user.Status.ToInternalStatus(), true);

    public static ContactStatus ToInternalStatus(this __OnLineStatus status) => status switch
    {
        __OnLineStatus.Online => ContactStatus.Online,
        __OnLineStatus.Offline => ContactStatus.Offline,
        __OnLineStatus.Away => ContactStatus.Away,
        __OnLineStatus.Busy => ContactStatus.Busy,
        __OnLineStatus.DoNotDisturb => ContactStatus.Dnd,
        __OnLineStatus.Invisible => ContactStatus.Invisible,
        __OnLineStatus.Unknown => throw new Exception("Why"),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}