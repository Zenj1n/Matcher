using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Neo4jClient.Cypher;
using Neo4jClient;
//using System.Runtime.Serialization.Json;

//author  Rinesh Ramadhin & Jason Lie Yung Tjong
//version 1.0, 08/10/14

namespace channel_tets
{
    class Program
    {

        static void Main(string[] args)
        {
            
            Console.WriteLine("Youtube channel checker v1 by Rinesh Ramadhin");
            Console.Title = "Youtube channel checker v1";
            Console.WriteLine("");
            Console.WriteLine("Druk op een toets om te starten");
            Console.ReadKey();
            Console.WriteLine("");
            Console.WriteLine("Starten...");
            Console.WriteLine("");


            createChannelNodes();
            Console.WriteLine("");
            Console.WriteLine("druk enter om af te sluiten");
            Console.Read();
        }

        static void createChannelNodes()
        {
            int channelCounter = 0;
            string currentChannel;
           // string channelsTxt = @"D:\Documents\Google Drive\Project 56 pcbuild.nl\Dev opdracht - Rinesh & Jason\channels.txt";
            int lineCount;
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();

            lineCount = 1;
           // lineCount = File.ReadLines(channelsTxt).Count();
            Console.WriteLine("voer channel naam in");
            string askChannel = Console.ReadLine();
            Console.WriteLine(askChannel);

            //while (channelCounter < lineCount && !Console.KeyAvailable) 
            while (channelCounter < lineCount && !Console.KeyAvailable) 
            {
                //currentChannel = File.ReadLines(channelsTxt).Skip(channelCounter).Take(1).First();
                currentChannel = askChannel;

                Console.WriteLine(currentChannel);
                var newChannel = new Channel { Name = currentChannel };
                 client.Cypher
                .Merge("(channel:Channel { Name: {name} })")
                .OnCreate()
                .Set("channel = {newChannel}")
                .WithParams(new
                {
                    name = newChannel.Name,
                    newChannel
                })
                .ExecuteWithoutResults();
                 Console.WriteLine(currentChannel + " toegevoegd");
                 createVideolNodes(currentChannel);
                 channelCounter++;
                 
            }
         }

        static void createVideolNodes(string currentChannel)
        {
            int videoCounter = 0;
            string channelUploadAPI;
            WebClient c = new WebClient();
            var videoClient = new GraphClient(new Uri("http://localhost:7474/db/data"));
            videoClient.Connect();
            string title;
            string published;
            string author;
            string videoId;

            while (videoCounter < 50)
            {
                try
                {
                    channelUploadAPI = "http://gdata.youtube.com/feeds/api/users/" + currentChannel + "/uploads?v=2&alt=json&max-results=50";
                    var data = c.DownloadString(channelUploadAPI);
                    JObject channelUploadJson = JObject.Parse(data);
                    title = (channelUploadJson["feed"]["entry"][videoCounter]["title"]["$t"]).ToString();
                    published = (channelUploadJson["feed"]["entry"][videoCounter]["published"]["$t"]).ToString();
                    videoId = (channelUploadJson["feed"]["entry"][videoCounter]["media$group"]["yt$videoid"]["$t"]).ToString();
                    author = currentChannel;

                 // maak video nodes aan

                    var newVideo = new Video {Id = videoId, Title = title, Date = published};
                    videoClient.Cypher
                   .Merge("(video:Video { Id: {videoId} })")
                   .OnCreate()
                   .Set("video = {newVideo}")
                   .WithParams(new
                    {
                      videoId = newVideo.Id,
                      newVideo
                    })
                   .ExecuteWithoutResults();
                    Console.WriteLine("Video van "+ currentChannel + " toegevoegd");
                   createCommentNodes(videoId);
                    
                //maak relaties tussen video en channel

                    videoClient.Cypher
                      .Match("(channel:Channel)", "(video:Video)")
                        .Where((Channel channel) => channel.Name == author)
                         .AndWhere((Video video) => video.Id == videoId)
                         .Create("channel-[:Author]->video")
                         .ExecuteWithoutResults();
                    Console.WriteLine("relatie gelegt tussen video en " + currentChannel);
                }
                catch
                {
                    Console.WriteLine("geen videos meer");
                    break;
                }
                videoCounter++;
                
            }
        }

        static void createCommentNodes(string currentVideo)
        {
            int commentCounter = 0;
            string videoCommentsAPI;
            WebClient c = new WebClient();
            var commentClient = new GraphClient(new Uri("http://localhost:7474/db/data"));
            commentClient.Connect();
            string content;
            string date;
            string author;
            string videoId;
            string commentId;

            while (commentCounter < 50)
            {
                try
                {
                    videoCommentsAPI = "http://gdata.youtube.com/feeds/api/videos/"+ currentVideo +"/comments??v=2&alt=json&max-results=50";
                    var data = c.DownloadString(videoCommentsAPI);
                    JObject videoCommentJson = JObject.Parse(data);
                    content = (videoCommentJson["feed"]["entry"][commentCounter]["content"]["$t"]).ToString();
                    date = (videoCommentJson["feed"]["entry"][commentCounter]["published"]["$t"]).ToString();
                    author = (videoCommentJson["feed"]["entry"][commentCounter]["author"][0]["name"]["$t"]).ToString();
                    commentId = (videoCommentJson["feed"]["entry"][commentCounter]["id"]["$t"]).ToString();
                    videoId = currentVideo;

                    

                    //  maak comment nodes aan

                    var newComment = new Comment { Content = content, Date = date, Id = commentId };
                    commentClient.Cypher
                   .Merge("(comment:Comment {Id: {commentId} })")
                   .OnCreate()
                   .Set("comment = {newComment}")
                   .WithParams(new
                   {
                       commentId = newComment.Id,
                       newComment
                   })
                   .ExecuteWithoutResults();
                    Console.WriteLine("Comment van " + currentVideo + " toegevoegd");

                  createUserNode(author, commentId);

                   //maak relaties tussen video en comment

                    commentClient.Cypher
                      .Match("(video:Video)", "(comment:Comment)")
                        .Where((Video video) => video.Id == videoId)
                         .AndWhere((Comment comment) => comment.Id == commentId)
                         .Create("video-[:PlacedOn]->comment")
                         .ExecuteWithoutResults();
                    Console.WriteLine("relatie gelegt tussen comment en " + currentVideo);
                    
                   
                }
                catch
                {
                    Console.WriteLine("geen comments meer");
                    break;
                }
                commentCounter++;
            }
        }

        static void createUserNode(string name, string commentId)
        {
            WebClient c = new WebClient();
            var userClient = new GraphClient(new Uri("http://localhost:7474/db/data"));
            userClient.Connect();
            var newUser = new User { Name = name};
            userClient.Cypher
           .Merge("(user:User {Name: {name} })")
           .OnCreate()
           .Set("user = {newUser}")
           .WithParams(new
           {
               name = newUser.Name,
               newUser
           })
           .ExecuteWithoutResults();
            Console.WriteLine( name + " toegevoegd");

            userClient.Cypher
                      .Match("(comment:Comment)", "(user:User)")
                        .Where((Comment comment) => comment.Id == commentId)
                         .AndWhere((User user) => user.Name == name)
                         .Create("user-[:WrittenBy]->comment")
                         .ExecuteWithoutResults();
            Console.WriteLine("relatie gelegt tussen comment en " + name);

        }

        public class Channel
        {
            public string Name { get; set; }
        }

        public class Video
        {
            public string Title { get; set; }
            public string Date { get; set; }
            public string Id { get; set; }
            public string Author { get; set; }
        }

        public class Comment
        {
            public string Content { get; set; }
            public string Date { get; set; }
            public string Id { get; set; }
        }

        public class User
        {
            public string Name { get; set; }
        }






        static long countString (string s)
        {

            long count = 1;
            int start = 0;

            while ((start = s.IndexOf('\n', start)) != -1)
            {

                count++;
                start++;

            }
            return count;

        }



    }
}