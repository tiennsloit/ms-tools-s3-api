namespace MS.S3API
{
    public class CreateMultipartUploadRequest
    {
        public required string BucketName { get; set; }
        public required string Key { get; set; }
    }
}
