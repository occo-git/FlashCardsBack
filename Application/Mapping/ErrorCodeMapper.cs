using Application.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mapping
{
    public static class ErrorCodeMapper 
    {
        public const string ErrorCode = "ErrorCode";

        private const string ErrAccountNotActive = "ERR_ACCOUNT_NOT_ACTIVE";
        private const string ErrClient = "ERR_CLIENT";
        private const string ErrConfirmationLinkMismatch = "ERR_CONFIRMATION_LINK_MISMATCH";
        private const string ErrConfirmationLinkRateLimit = "ERR_CONFIRMATION_LINK_RATE_LIMIT";
        private const string ErrEmailAlreadyConfirmed = "ERR_EMAIL_ALREADY_CONFIRMED";
        private const string ErrEmailNotConfirmed = "ERR_EMAIL_NOT_CONFIRMED";
        private const string ErrResetPasswordLinkMismatch = "ERR_RESET_PASSWORD_LINK_MISMATCH";
        private const string ErrTokenInvalidFormat = "ERR_TOKEN_INVALID_FORMAT";
        private const string GenericError = "ERR_ERROR";

        public static string Map(Exception? ex) => ex switch
        {
            null => String.Empty,
            AccountNotActiveException => ErrAccountNotActive,
            AppClientException => ErrClient,
            ConfirmationLinkRateLimitException => ErrConfirmationLinkRateLimit,
            ConfirmationLinkMismatchException => ErrConfirmationLinkMismatch,
            EmailAlreadyConfirmedException => ErrEmailAlreadyConfirmed,
            EmailNotConfirmedException => ErrEmailNotConfirmed,
            ResetPasswordLinkMismatchException => ErrResetPasswordLinkMismatch,
            TokenInvalidFormatException => ErrTokenInvalidFormat,
            _ => GenericError
        };
    }
}