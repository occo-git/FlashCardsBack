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
        private const string ErrConfirmationFailed = "ERR_CONFIRMATION_FAILED";
        private const string ErrConfirmationLinkMismatch = "ERR_CONFIRMATION_LINK_MISMATCH";
        private const string ErrConfirmationLinkRateLimit = "ERR_CONFIRMATION_LINK_RATE_LIMIT";
        private const string ErrConfirmationSendFail = "ERR_CONFIRMATION_SEND_FAIL";
        private const string ErrEmailAlreadyConfirmed = "ERR_EMAIL_ALREADY_CONFIRMED";
        private const string ErrEmailNotConfirmed = "ERR_EMAIL_NOT_CONFIRMED";
        private const string ErrTokenInvalidFormat = "ERR_TOKEN_INVALID_FORMAT";
        private const string GenericError = "ERR_ERROR";

        public static string Map(Exception? ex) => ex switch
        {
            null => String.Empty,
            AccountNotActiveException => ErrAccountNotActive,
            ConfirmationFailedException => ErrConfirmationFailed,
            ConfirmationLinkRateLimitException => ErrConfirmationLinkRateLimit,
            ConfirmationLinkMismatchException => ErrConfirmationLinkMismatch,
            ConfirmationSendFailException => ErrConfirmationSendFail,
            EmailAlreadyConfirmedException => ErrEmailAlreadyConfirmed,
            EmailNotConfirmedException => ErrEmailNotConfirmed,
            TokenInvalidFormatException => ErrTokenInvalidFormat,
            _ => GenericError
        };
    }
}