open System
open System.IO
open FSharp.Control.Tasks.V2
open Amazon.S3
open Amazon.S3.Model
open Amazon.S3.Transfer

type Config = {
    accessKeyId: string
    secretAccessKey: string
    serviceUrl: string
}

let createClient {accessKeyId = accessKeyId; secretAccessKey = secretAccessKey; serviceUrl = serviceUrl} =
    let config = AmazonS3Config(ServiceURL = serviceUrl, ForcePathStyle = true)
    new AmazonS3Client(accessKeyId, secretAccessKey, config)

let upload config (stream: Stream) (bucketName: string) (key: string) = task {
    use client = createClient config
    try
        let! _ = client.GetBucketLocationAsync bucketName
        ()
    with _ ->
        let! _ = client.PutBucketAsync bucketName
        ()
    use transferUtility = new TransferUtility(client)
    do! transferUtility.UploadAsync(stream, bucketName, key)
}

let uploadWithoutKey config stream bucketName = task {
    let key = Guid.NewGuid()
    do! upload config stream bucketName (string key)
    return key
}

let generatePreSignedUrl config bucketName key duration =
    use client = createClient config
    client.GetPreSignedURL(GetPreSignedUrlRequest(BucketName = bucketName, Key = key, Expires =  DateTime.Now.Add duration, Protocol = Protocol.HTTP))

let test () = task {
    let config = {
        accessKeyId = "ACCESS_KEY"
        secretAccessKey = "SECRET_KEY"
        serviceUrl = "http://minio:9000"
    }
    let bucketName = "deez"
    use stream = File.OpenRead "/workspace/test.txt"
    let! key = uploadWithoutKey config stream bucketName
    printfn "Uploaded %A: %A" key (generatePreSignedUrl config bucketName (string key) (TimeSpan.FromSeconds 20.))
}

[<EntryPoint>]
let main argv =
    test().Wait()
    printfn "Hello World from F#!"
    0
