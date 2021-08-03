﻿using Vismy.Core.Models.Implementations;
using Vismy.Core.Models.Interfaces;
using Vismy.Core.Models.Statuses;

#nullable disable

namespace Vismy.Core.Models.Joins
{
    public partial class UserPost : IEntity
    {
        public string UserId { get; set; }
        public int PostId { get; set; }
        public int UserPostStatusId { get; set; }

        public virtual Post Post { get; set; }
        public virtual AspNetUser User { get; set; }
        public virtual UserPostStatus UserPostStatus { get; set; }
    }
}
