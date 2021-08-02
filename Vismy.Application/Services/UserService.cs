﻿using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Vismy.Application.DTOs;
using Vismy.Application.Interfaces;
using Vismy.Core.Interfaces;
using Vismy.Core.Models.Implementations;
using Vismy.Core.Models.Joins;
using Vismy.Core.Models.Statuses;

namespace Vismy.Application.Services
{
    // TODO: define DTOs
    // TODO: define VMs
    // TODO: configure Model-DTO and DTO-VM maps
    // TODO: put VMs as methods parameters
    // TODO: add automapper service
    // TODO: add validation service for DTOs
    // TODO: get VM -> map into DTOs -> validate -> map into Models -> use repository
    // TODO: get Model -> map into DTOs -> do business logic -> map into VMs -> return to the Presentation
    public class UserService : IUserService
    {
        public IMapper Mapper { get; set; }
        public IRepository<Post> PostRepository { get; set; }
        public IRepository<Report> ReportRepository { get; set; }
        public IRepository<AspNetUser> UserRepository { get; set; }
        public IRepository<UserUser> UserUserRepository { get; set; }
        public IRepository<UserPost> UserPostRepository { get; set; }
        public IRepository<UserPostStatus> UserPostStatusRepository { get; set; }
        public IRepository<UserReportAuthor> UserReportAuthorRepository { get; set; }

        public UserService(
            IMapper mapper,
            IRepository<Post> postRepository,
            IRepository<Report> reportRepository,
            IRepository<AspNetUser> userRepository)
        {
            Mapper = mapper;
            PostRepository = postRepository;
            ReportRepository = reportRepository;
            UserRepository = userRepository;
        }

        public async Task AddPostAsync(PostInfoDTO postDto) => await PostRepository.AddAsync(Mapper.Map<Post>(postDto));

        public async Task EditPostAsync(PostInfoDTO postDto) => await PostRepository.UpdateAsync(Mapper.Map<Post>(postDto));

        public async Task DeletePostAsync(int postId) => await PostRepository.DeleteAsync(new Post() {Id = postId});

        public async Task FollowUserAsync(int userId, int followingId)
        {
            var userUser =
                (await UserUserRepository.GetAsync(uu => 
                    (uu.UserId == userId) && 
                    (uu.FollowerId == followingId)))
                .FirstOrDefault();

            if (userUser != null)
                await UserUserRepository.DeleteAsync(userUser);
            else
            {
                var user = await UserRepository.GetByIdAsync(userId, "UserUserUsers");
                var following = await UserRepository.GetByIdAsync(userId, "UserUserFollowers");

                userUser = new UserUser() { 
                    User = user, Follower = following,
                    UserId = user.Id, FollowerId = followingId };

                await UserUserRepository.AddAsync(userUser);

                user.UserUserUsers.Add(userUser);
                following.UserUserFollowers.Add(userUser);

                await UserRepository.UpdateAsync(user);
                await UserRepository.UpdateAsync(following);
            }
        }

        public async Task ViewPostAsync(int userId, int postId)
        {
            var userPost =
                (await UserPostRepository.GetAsync(uu => 
                    (uu.UserId == userId) && 
                    (uu.PostId == postId)))
                .FirstOrDefault();

            if (userPost == null)
            {
                var user = await UserRepository.GetByIdAsync(userId, "UserPosts");
                var post = await PostRepository.GetByIdAsync(postId, "UserPosts");
                var userPostStatus =
                    (await UserPostStatusRepository
                        .GetAsync(i => 
                            i.Name == "Viewed"))
                    .FirstOrDefault();

                userPost = new UserPost() { 
                    User = user, UserId = user.Id,
                    Post = post, PostId = postId, 
                    UserPostStatus = userPostStatus, UserPostStatusId = userPostStatus.Id };
                
                user.UserPosts.Add(userPost);
                post.UserPosts.Add(userPost);
            }
        }

        public async Task LikePostAsync(int userId, int postId)
        {
            var userPost = 
                (await UserPostRepository.GetAsync(uu => 
                    (uu.UserId == userId) && 
                    (uu.PostId == postId), 
                    "UserPostStatus"))
                .FirstOrDefault();
            
            if (userPost.UserPostStatus.Name == "Liked")
            {
                userPost.UserPostStatus.Name = "Viewed";
                userPost.UserPostStatus.Description = "View shows you viewed the post before.";
            }
            else
            {
                userPost.UserPostStatus.Name = "Liked";
                userPost.UserPostStatus.Description = "Like shows you appreciate the post.";
            }

            await UserPostRepository.UpdateAsync(userPost);
        }

        public async Task MakeReportAsync(int authorId, ReportInfoDTO reportDto)
        {
            var report = (await ReportRepository
                    .GetAsync(r => 
                        (r.PostId == reportDto.Post.Id) && 
                        (r.ReportStatus.Name == reportDto.TypeName), 
                        "UserReportAuthors"))
                .FirstOrDefault();
            var author = await UserRepository.GetByIdAsync(authorId, "");

            if (report == null)
            {
                report = Mapper.Map<Report>(reportDto);

                var userReportAuthor = new UserReportAuthor()
                {
                    Report = report,
                    ReportId = report.Id,
                    User = author,
                    UserId = authorId
                };
                await UserReportAuthorRepository.AddAsync(userReportAuthor);

                report.UserReportAuthors.Add(userReportAuthor);
                await ReportRepository.UpdateAsync(report);
            }
            else if(report.UserReportAuthors
                .Contains((await UserReportAuthorRepository
                    .GetAsync(ura => 
                        (ura.UserId == authorId) &&
                        (ura.ReportId == report.Id)))
                    .FirstOrDefault()))
            {
                var userReportAuthor = new UserReportAuthor()
                {
                    Report = report,
                    ReportId = report.Id,
                    User = author,
                    UserId = authorId
                };
                await UserReportAuthorRepository.AddAsync(userReportAuthor);

                report.UserReportAuthors.Add(userReportAuthor);
                await ReportRepository.UpdateAsync(report);
            }
        }
    }
}