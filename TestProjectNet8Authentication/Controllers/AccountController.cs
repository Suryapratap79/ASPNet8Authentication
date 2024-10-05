using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using MailKit;
using MailKit.Net.Smtp;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Tls;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TestProjectNet8Authentication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController(UserManager<IdentityUser> userManager) : ControllerBase
    {
        private async Task<IdentityUser?> GetUser(string email) => await userManager.FindByEmailAsync(email);
        [HttpPost("Register/{email}/{password}")]
        public async Task<IActionResult> Register(string email, string password)
        {
            await userManager.CreateAsync(new IdentityUser()
            {
                UserName = email,
                Email = email,
                PasswordHash = password
            }, password);
            await userManager.SetTwoFactorEnabledAsync(await GetUser(email), true);
            return Ok("Account created");
        }

        [HttpPost("Login/{email}/{password}")]
        public async Task<IActionResult> Login(string email, string password)
        {
            if(!await userManager.CheckPasswordAsync(await GetUser(email), password))            
                return Unauthorized();
            var token =await userManager.GenerateTwoFactorTokenAsync(await GetUser(email), TokenOptions.DefaultEmailProvider);
            return Ok(SendEmail(await  GetUser(email), token));
        }

        private object? SendEmail(IdentityUser? identityUser, string token)
        {
            StringBuilder emailbodyBuilder = new StringBuilder();
            emailbodyBuilder.AppendLine("<html>");
            emailbodyBuilder.AppendLine("<head>");
            emailbodyBuilder.AppendLine("<style>");
            emailbodyBuilder.AppendLine("h1 { color: #4CAF50; }"); // Green color for headings
            emailbodyBuilder.AppendLine("p { font-family: Arial, sans-serif; font-size: 14px; color: #333; }"); // Font styles for paragraphs
            emailbodyBuilder.AppendLine("</style>");
            emailbodyBuilder.AppendLine("</head>");
            emailbodyBuilder.AppendLine("<body>");
            //greeting
            emailbodyBuilder.AppendLine($"<h1>Dear {identityUser.Email},</h1>");

            //main content
            emailbodyBuilder.AppendLine("<p>Thank you for using our application.</p>");
            emailbodyBuilder.AppendLine($"<p>To complete your login process, please use the following verification</ p >");
            emailbodyBuilder.AppendLine($"code is <strong>{token}</strong>.</p>");
            emailbodyBuilder.AppendLine("<p>This code is valid for short period, so please use it promptly</p>");
           // emailbodyBuilder.AppendLine($"<p>Best regards,<br>{senderName}</p>");
            emailbodyBuilder.AppendLine("</body>");
            emailbodyBuilder.AppendLine("</html>");
            string message=emailbodyBuilder.ToString();
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse("michelle.corwin@ethereal.email"));
            email.To.Add(MailboxAddress.Parse("michelle.corwin@ethereal.email"));
            email.Subject = "2 FA Verification";
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message };
            using var smtp = new SmtpClient();
            smtp.Connect("smtp.ethereal.email", 587, MailKit.Security.SecureSocketOptions.StartTls);
            smtp.Authenticate("michelle.corwin@ethereal.email", "n7qGBbPfbU9jGSZY6h");
            smtp.Send(email);
            smtp.Disconnect(true);
            return "2FA verification code sent to your email, kindly check and verify";
        }

        [HttpPost("Verify2FA/{email}/{code}")]
        public async Task<IActionResult> Verify2FA(string email, string code)
        {
            await userManager.VerifyTwoFactorTokenAsync( await GetUser(email), TokenOptions.DefaultEmailProvider,code);
            return Ok(new[] { "Login successfully", Generatetoken(await GetUser(email)) });
        }

        private string Generatetoken(IdentityUser? identityUser)
        {
            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: [new Claim(JwtRegisteredClaimNames.Email, identityUser.Email)],
                expires: null,
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes("s8IK2mfGF4dSa39jeg3+3b5jmMYvRXolljyVgTRwUhbqzFtjstCGyan1oUZ+N5pXYKppcENSu+ivqBQLsUHThPqJ4wPzqg07Mts/0SijUMeYCrg1ABcLqC8FYtY+aAa5BXL4oHcEnsj36O+X2u8okJoRwK6zlZHXzSxryxnbrZo9No4nstZSra06wQtFui3Q\r\n")),
                SecurityAlgorithms.HmacSha256)
                );
            return new JwtSecurityTokenHandler().WriteToken(token);                
        }
    }
}
