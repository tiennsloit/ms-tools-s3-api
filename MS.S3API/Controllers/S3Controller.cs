using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;

namespace MS.S3API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class S3Controller : ControllerBase
    {
       
        private readonly ILogger<S3Controller> _logger;
        //ms-tool is the one for R2
        private const string BucketName = "ms-tool";//S3: "m-s-tools";
        private readonly IAmazonS3 _s3Client;


        public S3Controller(ILogger<S3Controller> logger, IAmazonS3 s3Client)
        {
            _logger = logger;
            _s3Client = s3Client;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Successs!");
        }

        [Route("upload")]
        [HttpPost]

        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            //read from stream
            using (var stream = file.OpenReadStream())
            {
                //upload to S3
                var downloadUrl = await OnPostAsync(file);
                return Ok(downloadUrl);
            }
        }

        public async Task<string?> OnPostAsync(IFormFile file)
        {
            string downloadUrl = string.Empty;
            if (file != null && file.Length > 0)
            {
                var fileKey = Path.GetFileName(file.FileName);
                var uploadRequest = new CreateMultipartUploadRequest
                {
                    BucketName = BucketName,
                    Key = fileKey,
                };

                var uploadResponse = await _s3Client.InitiateMultipartUploadAsync(uploadRequest.BucketName, uploadRequest.Key);
                var uploadId = uploadResponse.UploadId;

                var partETags = new List<PartETag>();

                try
                {
                    int partNumber = 1;
                    long filePosition = 0;
                    const int bufferSize = 5 * 1024 * 1024; // 5MB parts

                    while (filePosition < file.Length)
                    {
                        var buffer = new byte[bufferSize];
                        var bytesRead = await file.OpenReadStream().ReadAsync(buffer, 0, bufferSize);
                        using var stream = new MemoryStream(buffer, 0, bytesRead);

                        var uploadPartRequest = new UploadPartRequest
                        {
                            BucketName = BucketName,
                            Key = fileKey,
                            UploadId = uploadId,
                            PartNumber = partNumber,
                            PartSize = bytesRead,
                            InputStream = stream,
                            DisablePayloadSigning = true
                        };

                        var uploadPartResponse = await _s3Client.UploadPartAsync(uploadPartRequest);
                        partETags.Add(new PartETag()
                        {
                            PartNumber = uploadPartResponse.PartNumber,
                            ETag = uploadPartResponse.ETag,
                            ChecksumCRC32 = uploadPartResponse.ChecksumCRC32,
                            ChecksumCRC32C = uploadPartResponse.ChecksumCRC32C,
                            ChecksumSHA1 = uploadPartResponse.ChecksumSHA1,
                            ChecksumSHA256 = uploadPartResponse.ChecksumSHA256,
                           

                        });
                        partNumber++;
                        filePosition += bytesRead;
                    }

                    var completeMultipartUploadRequest = new CompleteMultipartUploadRequest
                    {
                        BucketName = BucketName,
                        Key = fileKey,
                        UploadId = uploadId,
                        PartETags = partETags,
                    };

                    var completeResult = await _s3Client.CompleteMultipartUploadAsync(completeMultipartUploadRequest);
                    //return the download url
                    downloadUrl = completeResult.Location;
                }
                catch
                {
                    await _s3Client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
                    {
                        BucketName = BucketName,
                        Key = fileKey,
                        UploadId = uploadId
                    });

                    downloadUrl = null;
                }
            }

            return downloadUrl;
        }
    }
}
