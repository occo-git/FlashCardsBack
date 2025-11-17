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
        public const string ErrAccountNotActive = "ERR_ACCOUNT_NOT_ACTIVE";
        public const string ErrEmailAlreadyConfirmed = "ERR_EMAIL_ALREADY_CONFIRMED";
        public const string ErrEmailNotConfirmed = "ERR_EMAIL_NOT_CONFIRMED";
        public const string ErrFailSendConfirmation = "ERR_FAIL_SEND_CONFIRMATION";
        public const string ErrInvalidTokenFormat = "ERR_INVALID_TOKEN_FORMAT";
        public const string GenericError = "ERR_ERROR";

        public static string Map(Exception? ex) => ex switch
        {
            null => String.Empty,
            AccountNotActiveException => ErrAccountNotActive,
            EmailAlreadyConfirmedException => ErrEmailAlreadyConfirmed,
            EmailNotConfirmedException => ErrEmailNotConfirmed,
            FailSendConfirmationException => ErrFailSendConfirmation,
            InvalidTokenFormatException => ErrInvalidTokenFormat,
            _ => GenericError
        };
    }
}