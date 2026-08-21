using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Khoon_e_Hayat.Data;
using Khoon_e_Hayat.Models.Entities;

namespace Khoon_e_Hayat.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;

        public EmailService(IConfiguration config, ApplicationDbContext context)
        {
            _config = config;
            _context = context;
        }

        // ==========================================
        // CORE METHOD - Required by all other methods
        // ==========================================
        public async Task SendEmailAsync(string toEmail, string subject, string body, string category = "General")
        {
            var log = new NotificationLog
            {
                RecipientEmail = toEmail,
                NotificationType = "Email",
                Subject = subject,
                Message = body,
                Status = "Pending",
                Category = category,
                SentAt = DateTime.Now
            };

            try
            {
                var smtpConfig = _config.GetSection("SmtpSettings");
                using var smtpClient = new SmtpClient(smtpConfig["Server"])
                {
                    Port = int.Parse(smtpConfig["Port"]),
                    Credentials = new NetworkCredential(smtpConfig["Username"], smtpConfig["Password"]),
                    EnableSsl = bool.Parse(smtpConfig["EnableSSL"]),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpConfig["SenderEmail"], smtpConfig["SenderName"]),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                log.Status = "Sent";
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.ErrorMessage = ex.Message;
            }
            finally
            {
                _context.NotificationLogs.Add(log);
                await _context.SaveChangesAsync();
            }
        }

        // ==========================================
        // AUTHENTICATION & ACCOUNT EMAILS
        // ==========================================
        public async Task SendEmailVerificationAsync(string toEmail, string fullName, string verificationLink)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Verify Your Email</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #90151C; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .btn {{ display: inline-block; background-color: #198754; color: #ffffff !important; text-decoration: none; padding: 12px 30px; border-radius: 8px; font-weight: 600; font-size: 16px; margin: 20px 0; text-align: center; }}
        .btn:hover {{ background-color: #146c43; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>Welcome to Khoon-e-Hayat!</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{fullName}</strong>,</p>
            <p>Thank you for registering with Khoon-e-Hayat. To complete your registration and activate your account, please verify your email address by clicking the button below:</p>
            <div style='text-align: center;'><a href='{verificationLink}' class='btn'>Verify Email Address</a></div>
            <p style='font-size: 14px; color: #6c757d; margin-top: 20px;'><strong>Note:</strong> This link will expire in 24 hours. If you did not create an account, please ignore this email.</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, "Verify Your Email - Khoon-e-Hayat", body, "Authentication");
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset Request</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #5C88A8; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .btn {{ display: inline-block; background-color: #90151C; color: #ffffff !important; text-decoration: none; padding: 12px 30px; border-radius: 8px; font-weight: 600; font-size: 16px; margin: 20px 0; text-align: center; }}
        .btn:hover {{ background-color: #7a1218; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>Password Reset Request</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{fullName}</strong>,</p>
            <p>We received a request to reset your password. Click the button below to create a new password for your account:</p>
            <div style='text-align: center;'><a href='{resetLink}' class='btn'>Reset Password</a></div>
            <p style='font-size: 14px; color: #6c757d; margin-top: 20px;'><strong>Note:</strong> This link will expire in 1 hour. If you did not request this, please ignore this email.</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, "Password Reset Request - Khoon-e-Hayat", body, "Authentication");
        }

        // ==========================================
        // HOSPITAL VERIFICATION EMAILS
        // ==========================================
        public async Task SendHospitalApprovalEmailAsync(string toEmail, string hospitalName)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Hospital Account Approved</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #198754; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #f0fdf4; border-left: 4px solid #198754; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #198754; font-weight: 500; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>🎉 Hospital Account Approved</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{hospitalName}</strong>,</p>
            <p>We are pleased to inform you that your hospital account has been reviewed and approved by the Khoon-e-Hayat Administration Team.</p>
            <div class='info-box'>
                <p>✅ You can now access your Hospital Dashboard.</p>
                <p>✅ All hospital features and donor management tools are available.</p>
            </div>
            <p>Thank you for joining Khoon-e-Hayat and supporting our blood donation services.</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, "Hospital Account Approved - Khoon-e-Hayat", body, "HospitalVerification");
        }

        public async Task SendHospitalRejectionEmailAsync(string toEmail, string hospitalName, string reason)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Hospital Account Verification Update</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #90151C; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #fff5f5; border-left: 4px solid #90151C; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #90151C; font-weight: 500; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>Hospital Account Verification Update</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{hospitalName}</strong>,</p>
            <p>We regret to inform you that your hospital account verification request has been rejected by the Khoon-e-Hayat Administration Team.</p>
            <div class='info-box'>
                <p><strong>Reason for Rejection:</strong><br>{reason}</p>
            </div>
            <p>If you believe this is a mistake or if you can rectify the issue, please update your profile information and documents, or contact our support team for further assistance.</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, "Hospital Account Rejected - Khoon-e-Hayat", body, "HospitalVerification");
        }

        // ==========================================
        // EMERGENCY ALERT EMAILS
        // ==========================================
        public async Task SendEmergencyDonorNotificationAsync(string toEmail, string donorName, string bloodGroup, string hospitalName, string city, DateTime? requiredDate, string urgencyLevel, string alertMessage)
        {
            var reqDate = requiredDate?.ToString("dd-MMM-yyyy") ?? "As soon as possible";
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Emergency Blood Requirement</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #90151C; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #fff5f5; border-left: 4px solid #90151C; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .btn {{ display: inline-block; background-color: #90151C; color: #ffffff !important; text-decoration: none; padding: 12px 30px; border-radius: 8px; font-weight: 600; font-size: 16px; margin: 20px 0; text-align: center; }}
        .btn:hover {{ background-color: #7a1218; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
        .highlight {{ color: #90151C; font-weight: 700; font-size: 1.1em; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>🚨 Emergency Blood Requirement</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{donorName}</strong>,</p>
            <p>An urgent blood requirement has been raised in your city. As a registered donor, your help is desperately needed to save a life.</p>
            <div class='info-box'>
                <p><strong>Blood Group Required:</strong> <span class='highlight'>{bloodGroup}</span></p>
                <p><strong>Hospital:</strong> {hospitalName}</p>
                <p><strong>City:</strong> {city}</p>
                <p><strong>Required By:</strong> {reqDate}</p>
                <p><strong>Urgency Level:</strong> {urgencyLevel}</p>
                <p style='margin-top: 15px; border-top: 1px solid #fecaca; padding-top: 10px;'><strong>Message:</strong> {alertMessage}</p>
            </div>
            <div style='text-align: center;'><a href='https://localhost:7182/Account/Login' class='btn'>View Request & Respond</a></div>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, $"🚨 URGENT: {bloodGroup} Blood Required in {city}", body, "EmergencyAlert");
        }

        public async Task SendHospitalEmergencyNotificationAsync(string toEmail, string hospitalName, int alertId, string bloodGroup, int unitsRequired, string receiverName, string urgencyLevel, DateTime? requiredDate)
        {
            var reqDate = requiredDate?.ToString("dd-MMM-yyyy") ?? "As soon as possible";
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Emergency Alert Notification</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #5C88A8; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #e6f4ff; border-left: 4px solid #5C88A8; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
        .highlight {{ color: #90151C; font-weight: 700; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>🏥 Emergency Alert Notification</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{hospitalName}</strong> Administration,</p>
            <p>This is an official notification from Khoon-e-Hayat regarding an emergency blood request registered for your hospital.</p>
            <div class='info-box'>
                <p><strong>Alert ID:</strong> ALT-{alertId:D4}</p>
                <p><strong>Blood Group:</strong> <span class='highlight'>{bloodGroup}</span></p>
                <p><strong>Units Required:</strong> {unitsRequired}</p>
                <p><strong>Receiver:</strong> {receiverName}</p>
                <p><strong>Urgency Level:</strong> {urgencyLevel}</p>
                <p><strong>Required By:</strong> {reqDate}</p>
            </div>
            <p>Please ensure the necessary arrangements are made on your end. Our system is actively notifying compatible donors in your area.</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, $"Emergency Alert ALT-{alertId:D4} - {bloodGroup} Blood Required", body, "EmergencyAlert");
        }

        // ==========================================
        // SMART DONOR MATCHING & WORKFLOW EMAILS
        // ==========================================
        public async Task SendDonationRequestEmailAsync(string toEmail, string donorName, int requestId, string bloodGroup, string hospitalName)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Blood Donation Request</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #90151C; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #f0f4f8; border-left: 4px solid #5C88A8; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .btn {{ display: inline-block; background-color: #90151C; color: #ffffff !important; text-decoration: none; padding: 12px 30px; border-radius: 8px; font-weight: 600; font-size: 16px; margin: 20px 0; text-align: center; }}
        .btn:hover {{ background-color: #7a1218; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
        .highlight {{ color: #90151C; font-weight: 700; font-size: 1.1em; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>Blood Donation Request</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{donorName}</strong>,</p>
            <p>We have a critical request for blood at your nearby hospital. Your profile matches the requirements, and your help can save a life.</p>
            <div class='info-box'>
                <p><strong>Request ID:</strong> REQ-{requestId:D4}</p>
                <p><strong>Blood Group Required:</strong> <span class='highlight'>{bloodGroup}</span></p>
                <p><strong>Hospital:</strong> {hospitalName}</p>
            </div>
            <div style='text-align: center;'><a href='https://localhost:7182/Account/Login' class='btn'>View Request & Respond</a></div>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, $"Donation Request: {bloodGroup} Blood Required", body, "SmartMatch");
        }

        public async Task SendDonorSelectedEmailAsync(string toEmail, string donorName, int requestId, string bloodGroup)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>You Have Been Selected!</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #198754; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #f0fdf4; border-left: 4px solid #198754; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>🎉 You Have Been Selected!</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{donorName}</strong>,</p>
            <p>Congratulations! You have been selected as the primary donor for the following blood request. Our hospital team will contact you shortly with the donation schedule.</p>
            <div class='info-box'>
                <p><strong>Request ID:</strong> REQ-{requestId:D4}</p>
                <p><strong>Blood Group:</strong> {bloodGroup}</p>
            </div>
            <p>Please prepare for the donation and ensure you are well-hydrated. Thank you for being a hero!</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, $"Selected as Donor for Request REQ-{requestId:D4}", body, "SmartMatch");
        }

        public async Task SendDonationScheduledEmailAsync(string toEmail, string donorName, DateTime scheduledDate, string hospitalName)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Donation Scheduled</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #5C88A8; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #e6f4ff; border-left: 4px solid #5C88A8; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>📅 Donation Scheduled</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{donorName}</strong>,</p>
            <p>Your blood donation has been successfully scheduled. Please arrive 15 minutes early and remember to stay hydrated before your appointment.</p>
            <div class='info-box'>
                <p><strong>Scheduled Date & Time:</strong> {scheduledDate:dd-MMM-yyyy hh:mm tt}</p>
                <p><strong>Hospital:</strong> {hospitalName}</p>
            </div>
            <p>If you need to reschedule or have any questions, please contact the hospital directly.</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, "Your Donation Schedule", body, "SmartMatch");
        }

        public async Task SendRequestCancelledEmailAsync(string toEmail, string donorName, int requestId, string reason)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Request Cancelled</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #6c757d; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #f8f9fa; border-left: 4px solid #6c757d; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>Request Cancelled</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{donorName}</strong>,</p>
            <p>The blood donation request you were assigned to has been cancelled by the hospital.</p>
            <div class='info-box'>
                <p><strong>Request ID:</strong> REQ-{requestId:D4}</p>
                <p><strong>Reason:</strong> {reason}</p>
            </div>
            <p>Thank you for your willingness to help. We hope to connect you with another life-saving opportunity soon.</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, $"Request REQ-{requestId:D4} Cancelled", body, "SmartMatch");
        }

        // ==========================================
        // EMERGENCY WORKFLOW EMAILS
        // ==========================================
        public async Task SendEmergencyVolunteerConfirmationToDonorAsync(string toEmail, string donorName, int requestId, string bloodGroup, string hospitalName)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Emergency Response Received</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #5C88A8; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #e6f4ff; border-left: 4px solid #5C88A8; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>Emergency Response Received</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{donorName}</strong>,</p>
            <p>Thank you for responding to the emergency blood request. Your response has been successfully received.</p>
            <div class='info-box'>
                <p><strong>Request ID:</strong> REQ-{requestId:D4}</p>
                <p><strong>Blood Group:</strong> {bloodGroup}</p>
                <p><strong>Hospital:</strong> {hospitalName}</p>
            </div>
            <p>Our hospital team is currently reviewing your availability and will contact you shortly if you are selected as the primary donor.</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, $"Emergency Response Received - REQ-{requestId:D4}", body, "EmergencyAlert");
        }

        public async Task SendNewVolunteerNotificationToHospitalAsync(string toEmail, string hospitalName, int requestId, string bloodGroup, string donorName, string donorPhone, double distanceKm, string travelTime)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>New Donor Volunteered</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #5C88A8; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #e6f4ff; border-left: 4px solid #5C88A8; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>New Donor Volunteered</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{hospitalName}</strong>,</p>
            <p>A donor has volunteered for your emergency request. Please review their profile and confirm if they are suitable for the donation.</p>
            <div class='info-box'>
                <p><strong>Request ID:</strong> REQ-{requestId:D4} ({bloodGroup})</p>
                <p><strong>Donor Name:</strong> {donorName}</p>
                <p><strong>Contact:</strong> {donorPhone}</p>
                <p><strong>Distance:</strong> {distanceKm} km (ETA: {travelTime})</p>
            </div>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, $"New Volunteer for Request REQ-{requestId:D4}", body, "EmergencyAlert");
        }

        public async Task SendEmergencyDonorSelectedAsync(string toEmail, string donorName, int requestId, string bloodGroup, string hospitalName, DateTime scheduledDate)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>You Have Been Selected!</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #198754; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #f0fdf4; border-left: 4px solid #198754; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>🎉 You Have Been Selected!</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{donorName}</strong>,</p>
            <p>You have been selected as the primary donor for the following emergency request. Please prepare for the donation.</p>
            <div class='info-box'>
                <p><strong>Request ID:</strong> REQ-{requestId:D4} ({bloodGroup})</p>
                <p><strong>Hospital:</strong> {hospitalName}</p>
                <p><strong>Scheduled Date:</strong> {scheduledDate:dd-MMM-yyyy hh:mm tt}</p>
            </div>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, $"Selected for Emergency Request REQ-{requestId:D4}", body, "EmergencyAlert");
        }

        public async Task SendEmergencyDonationCompletedToDonorAsync(string toEmail, string donorName, int requestId)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Thank You for Your Donation!</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #198754; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #f0fdf4; border-left: 4px solid #198754; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>🎉 Thank You for Your Donation!</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{donorName}</strong>,</p>
            <p>Thank you for successfully completing the emergency donation. Your incredible contribution will help save lives.</p>
            <div class='info-box'>
                <p><strong>Request ID:</strong> REQ-{requestId:D4}</p>
                <p><strong>Status:</strong> <span style='color: #198754; font-weight: 600;'>Completed Successfully</span></p>
            </div>
            <p>Please ensure you rest well and stay hydrated. We are deeply grateful for your generosity.</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, $"Emergency Donation Completed - REQ-{requestId:D4}", body, "EmergencyAlert");
        }

        public async Task SendEmergencyDonationCompletedToReceiverAsync(string toEmail, string receiverName, int requestId, string donorName)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Donation Completed Successfully</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #198754; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #f0fdf4; border-left: 4px solid #198754; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>🎉 Donation Completed Successfully</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{receiverName}</strong>,</p>
            <p>We are pleased to inform you that a compatible donor has successfully completed the blood donation for your request.</p>
            <div class='info-box'>
                <p><strong>Request ID:</strong> REQ-{requestId:D4}</p>
                <p><strong>Donor:</strong> {donorName}</p>
                <p><strong>Status:</strong> <span style='color: #198754; font-weight: 600;'>Fulfilled</span></p>
            </div>
            <p>We wish you a speedy recovery. Please contact the hospital for any further medical procedures.</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, $"Emergency Donation Completed - REQ-{requestId:D4}", body, "EmergencyAlert");
        }

        public async Task SendEmergencyDonationCompletedToHospitalAsync(string toEmail, string hospitalName, int requestId, string donorName)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Donation Completed Successfully</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #198754; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #f0fdf4; border-left: 4px solid #198754; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>🎉 Donation Completed Successfully</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{hospitalName}</strong>,</p>
            <p>We are pleased to inform you that the emergency blood donation for the following request has been successfully completed.</p>
            <div class='info-box'>
                <p><strong>Request ID:</strong> REQ-{requestId:D4}</p>
                <p><strong>Donor:</strong> {donorName}</p>
                <p><strong>Status:</strong> <span style='color: #198754; font-weight: 600;'>Fulfilled</span></p>
            </div>
            <p>Please update your inventory records accordingly. Thank you for using Khoon-e-Hayat.</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, $"Emergency Donation Completed - REQ-{requestId:D4}", body, "EmergencyAlert");
        }

        // ==========================================
        // RECEIVER WORKFLOW EMAILS
        // ==========================================
        public async Task SendBloodReadyForCollectionEmailAsync(string toEmail, string receiverName, string hospitalName, string bloodGroup, int units, DateTime issueDate, int requestId)
        {
            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Blood Ready for Collection</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #198754; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #f0fdf4; border-left: 4px solid #198754; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>🩸 Blood Ready for Collection</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{receiverName}</strong>,</p>
            <p>Your requested blood has been successfully prepared and is ready for collection.</p>
            <div class='info-box'>
                <p><strong>Hospital:</strong> {hospitalName}</p>
                <p><strong>Blood Group:</strong> {bloodGroup}</p>
                <p><strong>Units:</strong> {units}</p>
                <p><strong>Issue Date:</strong> {issueDate:dd-MMM-yyyy, hh:mm tt}</p>
                <p><strong>Request ID:</strong> REQ-{requestId:D4}</p>
            </div>
            <p>Please visit the hospital during working hours with your <strong>CNIC</strong> and <strong>Request ID</strong> to collect your blood.</p>
            <p>Thank you for using Khoon-e-Hayat.</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, "Blood Ready for Collection - Khoon-e-Hayat", body, "BloodReady");
        }

        // ==========================================
        // PROFESSIONAL DONOR REMINDER EMAIL
        // ==========================================
        public async Task SendDonationReminderEmailAsync(
            string toEmail,
            string donorName,
            int requestId,
            string bloodGroup,
            string patientName,
            string hospitalName,
            string status,
            string customMessage = "")
        {
            var customMsgHtml = !string.IsNullOrEmpty(customMessage)
                ? $"<div class='custom-msg'><strong>Message from Hospital:</strong><br>{customMessage}</div>"
                : "";

            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Blood Donation Reminder</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333333; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .email-wrapper {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
        .email-header {{ background-color: #5C88A8; color: #ffffff; padding: 30px 20px; text-align: center; }}
        .email-header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .email-body {{ padding: 30px 40px; }}
        .email-body p {{ margin-bottom: 15px; color: #555555; }}
        .info-box {{ background-color: #f0f4f8; border-left: 4px solid #5C88A8; padding: 15px 20px; border-radius: 6px; margin: 20px 0; }}
        .info-box p {{ margin: 8px 0; color: #333333; }}
        .btn {{ display: inline-block; background-color: #90151C; color: #ffffff !important; text-decoration: none; padding: 12px 30px; border-radius: 8px; font-weight: 600; font-size: 16px; margin: 20px 0; text-align: center; }}
        .btn:hover {{ background-color: #7a1218; }}
        .custom-msg {{ background-color: #e6f4ff; border: 1px solid #b3d9ff; padding: 15px; border-radius: 8px; margin: 20px 0; font-style: italic; color: #555; }}
        .email-footer {{ background-color: #f8f9fa; padding: 25px 40px; text-align: center; border-top: 1px solid #e9ecef; }}
        .email-footer p {{ margin: 5px 0; font-size: 13px; color: #6c757d; }}
        .highlight {{ color: #90151C; font-weight: 700; }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-header'><h1>Donation Reminder</h1></div>
        <div class='email-body'>
            <p>Dear <strong>{donorName}</strong>,</p>
            <p>This is a friendly reminder regarding your blood donation request. Your contribution can make a significant difference in someone's life. Please take a moment to review the details below and respond at your earliest convenience.</p>
            
            <div class='info-box'>
                <p><strong>Request ID:</strong> REQ-{requestId:D4}</p>
                <p><strong>Patient Name:</strong> {patientName}</p>
                <p><strong>Blood Group Required:</strong> <span class='highlight'>{bloodGroup}</span></p>
                <p><strong>Hospital:</strong> {hospitalName}</p>
                <p><strong>Current Status:</strong> {status}</p>
            </div>

            {customMsgHtml}

            <div style='text-align: center;'><a href='https://localhost:7182/Account/Login' class='btn'>View Request & Respond</a></div>

            <p style='margin-top: 25px;'>If you have any questions or concerns, please don't hesitate to contact the hospital directly. Your generosity and willingness to help are greatly appreciated.</p>
        </div>
        <div class='email-footer'>
            <p style='font-weight: 600; color: #90151C; font-size: 16px; margin-bottom: 10px;'>Khoon-e-Hayat</p>
            <p>Every Drop Saves a Life ❤️</p>
            <p>© {DateTime.Now.Year} Khoon-e-Hayat Blood Donation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, $"Reminder: Blood Donation Request for {patientName}", body, "DonorReminder");
        }
    }
}