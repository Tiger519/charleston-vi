using System;
using System.IO;
using System.Drawing;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Charleston.Detect
{
    public static class analyzeImage
    {
        static CustomVisionPredictionClient prediction_client;

        [FunctionName("analyzeImage")]

        public static void Run([BlobTrigger("images/{name}", Connection = "charlestonstorage_STORAGE")]Stream myBlob, string name, [Blob("processed/{name}", FileAccess.Write, Connection = "charlestonstorage_STORAGE")]Stream outputBlob, ILogger log) // Not sure if outputBlob will work
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            try
            {
                // Get Configuration Settings
                string prediction_endpoint = System.Environment.GetEnvironmentVariable("PredictionEndpoint");
                log.LogInformation($"prediction_endpoint {prediction_endpoint}");
                string prediction_key = System.Environment.GetEnvironmentVariable("PredictionKey");
                log.LogInformation($"prediction_key {prediction_key}");
                Guid project_id = Guid.Parse(System.Environment.GetEnvironmentVariable("ProjectID"));
                log.LogInformation($"project_id {project_id}");
                string model_name = System.Environment.GetEnvironmentVariable("ModelName");
                log.LogInformation($"model_name {model_name}");

                // Authenticate a client for the prediction API
                prediction_client = new CustomVisionPredictionClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.ApiKeyServiceClientCredentials(prediction_key))
                {
                    Endpoint = prediction_endpoint
                };

                // Load the image and prepare for drawing
                //String image_file = "testPartImage2.png";
                //Image image = Image.FromFile(image_file);
                Image image = Image.FromStream(myBlob);
                int h = image.Height;
                int w = image.Width;
                log.LogInformation($"Image dimensions {h}h x {w}w");
                Graphics graphics = Graphics.FromImage(image);
                Pen pen = new Pen(Color.Red, 3);
                Font font = new Font("Arial", 12);
                SolidBrush brush = new SolidBrush(Color.Red);

                
                //using (var image_data = File.OpenRead(image_file))
                /*using (var image_data = myBlob)
                {
                    // Make a prediction against the new project
                    Console.WriteLine("Detecting objects in " + myBlob);
                    Console.WriteLine(project_id + " " + model_name);
                    var result = prediction_client.DetectImage(project_id, model_name, image_data);*/

                    log.LogInformation("Detecting objects in " + myBlob);
                    log.LogInformation($"{project_id} {model_name} images/{name}");
                    string BlobSAS = System.Environment.GetEnvironmentVariable("BlobSAS");
                    log.LogInformation(BlobSAS);
                    var image_url = new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImageUrl($"https://charlestonstorage.blob.core.windows.net/images/{name}?{BlobSAS}");
                    var result = prediction_client.DetectImageUrl(project_id, model_name, image_url);

                    // Loop over each prediction
                    foreach (var prediction in result.Predictions)
                    {
                        // Get each prediction with a probability > 30%
                        if (prediction.Probability > 0.3)
                        {
                            // The bounding box sizes are proportional - convert to absolute
                            int left = Convert.ToInt32(prediction.BoundingBox.Left * w);
                            int top = Convert.ToInt32(prediction.BoundingBox.Top * h);
                            int height = Convert.ToInt32(prediction.BoundingBox.Height * h);
                            int width =  Convert.ToInt32(prediction.BoundingBox.Width * w);
                            // Draw the bounding box
                            Rectangle rect = new Rectangle(left, top, width, height);
                            graphics.DrawRectangle(pen, rect);
                            // Annotate with the predicted label
                            graphics.DrawString(prediction.TagName,font,brush,left,top);
                
                        }
                    }
                //}
                // Save the annotated image
                String output_file = "output.jpg";
                image.Save(output_file);
                MemoryStream ms = new MemoryStream();
                image.Save(ms, image.RawFormat);
                //myBlob.CopyTo(outputBlob);
                ms.CopyTo(outputBlob);
                Console.WriteLine("Results saved in " + output_file);
                log.LogInformation($"Results saved in {output_file}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                log.LogInformation($"Error: {ex}");
            }
        }
    }
}
