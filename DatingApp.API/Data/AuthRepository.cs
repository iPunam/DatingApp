using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DataContext _context;
        public AuthRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<User> Register(User user, string password)
        {
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return user;
        }
        public async Task<User> LogIn(string userName, string password)
        {
            User user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                return null;
            }

            if (!ValidatePassword(password, user.PasswordHash, user.PasswordSalt))
            {
                return null;
            }
            return user;
        }
        public async Task<bool> UserExists(string userName)
        {
            User user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                return false;
            }
            return true;
        }
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }
        private bool ValidatePassword(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (passwordHash.Length > i)
                    {
                        if (computedHash[i] != passwordHash[i])
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
    }
}
