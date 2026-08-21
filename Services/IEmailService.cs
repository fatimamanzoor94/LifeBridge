using System;
using System.Threading.Tasks;

namespace Khoon_e_Hayat.Services
{
    public interface IEmailService
    {
        // ==========================================
        // CORE METHOD
        // ==========================================
        Task SendEmailAsync(string toEmail, string subject, string body, string category = "General");

        // ==========================================
        // AUTHENTICATION & ACCOUNT EMAILS
        // ==========================================
        Task SendEmailVerificationAsync(string toEmail, string fullName, string verificationLink);
        Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink);

        // ==========================================
        // HOSPITAL VERIFICATION EMAILS
        // ==========================================
        Task SendHospitalApprovalEmailAsync(string toEmail, string hospitalName);
        Task SendHospitalRejectionEmailAsync(string toEmail, string hospitalName, string reason);

        // ==========================================
        // EMERGENCY ALERT EMAILS
        // ==========================================
        Task SendEmergencyDonorNotificationAsync(string toEmail, string donorName, string bloodGroup, string hospitalName, string city, DateTime? requiredDate, string urgencyLevel, string alertMessage);
        Task SendHospitalEmergencyNotificationAsync(string toEmail, string hospitalName, int alertId, string bloodGroup, int unitsRequired, string receiverName, string urgencyLevel, DateTime? requiredDate);

        // ==========================================
        // SMART DONOR MATCHING & WORKFLOW EMAILS
        // ==========================================
        Task SendDonationRequestEmailAsync(string toEmail, string donorName, int requestId, string bloodGroup, string hospitalName);
        Task SendDonorSelectedEmailAsync(string toEmail, string donorName, int requestId, string bloodGroup);
        Task SendDonationScheduledEmailAsync(string toEmail, string donorName, DateTime scheduledDate, string hospitalName);
        Task SendRequestCancelledEmailAsync(string toEmail, string donorName, int requestId, string reason);

        // ==========================================
        // EMERGENCY WORKFLOW EMAILS
        // ==========================================
        Task SendEmergencyVolunteerConfirmationToDonorAsync(string toEmail, string donorName, int requestId, string bloodGroup, string hospitalName);
        Task SendNewVolunteerNotificationToHospitalAsync(string toEmail, string hospitalName, int requestId, string bloodGroup, string donorName, string donorPhone, double distanceKm, string travelTime);
        Task SendEmergencyDonorSelectedAsync(string toEmail, string donorName, int requestId, string bloodGroup, string hospitalName, DateTime scheduledDate);
        Task SendEmergencyDonationCompletedToDonorAsync(string toEmail, string donorName, int requestId);
        Task SendEmergencyDonationCompletedToReceiverAsync(string toEmail, string receiverName, int requestId, string donorName);
        Task SendEmergencyDonationCompletedToHospitalAsync(string toEmail, string hospitalName, int requestId, string donorName);

        // ==========================================
        // RECEIVER WORKFLOW EMAILS
        // ==========================================
        Task SendBloodReadyForCollectionEmailAsync(string toEmail, string receiverName, string hospitalName, string bloodGroup, int units, DateTime issueDate, int requestId);

        // ==========================================
        // PROFESSIONAL DONOR REMINDER EMAIL
        // ==========================================
        Task SendDonationReminderEmailAsync(
            string toEmail,
            string donorName,
            int requestId,
            string bloodGroup,
            string patientName,
            string hospitalName,
            string status,
            string customMessage = "");
    }
}