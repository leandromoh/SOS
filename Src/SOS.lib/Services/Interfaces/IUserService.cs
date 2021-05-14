﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SOS.Lib.Models.UserService;

namespace SOS.Lib.Services.Interfaces
{
    /// <summary>
    ///     Interface for user service
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        ///  Get current user
        /// </summary>
        /// <returns></returns>
        Task<UserModel> GetUserAsync();

        /// <summary>
        ///  Get authorities for user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="authorizationApplicationIdentifier"></param>
        /// <returns></returns>
        Task<IEnumerable<AuthorityModel>> GetUserAuthoritiesAsync(int userId, string authorizationApplicationIdentifier = null);

        /// <summary>
        /// Get user roles
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="authorizationApplicationIdentifier"></param>
        /// <returns></returns>
        Task<IEnumerable<RoleModel>> GetUserRolesAsync(int userId, string authorizationApplicationIdentifier = null);
    }
}