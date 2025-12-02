namespace Shared
{
    public static class SharedConstants
    {
        public const string ApiTitle = "FlashCards API";
        public const string ApiVersion = "v1";

        public const string ApiSettings = "ApiSettings";
        public const string ApiTokenOptions = "ApiTokenOptions";
        public const string JwtValidationOptions = "JwtValidationOptions";

        public const string EnvJwtSecret = "JWT_SECRET_KEY";
        public const string EnvApiGroup = "API";
        public const string EnvRedisGroup = "REDIS";
        public const string EnvSmtpGroup = "SMTP";
        public const string EnvRateLimitGroup = "RATE_LIMIT";
        public const string EnvFileStoragePath = "FILE_STORAGE_PATH";

        public const string FlashCardsConnectionString = "FlashCardsConnectionString";
        public const string Migrate = "migrate";

        public const string RateLimitAuthPolicy = "RateLimitAuthPolicy";
    }
}
