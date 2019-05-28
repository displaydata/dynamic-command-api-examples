using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DynamicCommand;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CodeExamples
{
    [TestFixture]
    public class ApiExamples
    {
        // ----------------------------------------------------------------
        //Change the values below so they are relevant to your deployment of Dynamic Solution.
        private static readonly string ApiServer = "https://192-168-56-3.ip.dev.zbddisplays.local";
        private readonly Location existingLocation1 = new Location
        {
            Name = "Location_001",
        };
        //Change for the AddDisplay and DeleteDisplay examples to use a different set of display serial numbers that you can add and remove displays to the system.
        //The existingLocation1 above will require a working communicator for example code to work.
        List<Display> displaysToUse = new List<Display> {
            new Display { SerialNumber = "JA00000001B" },
            new Display { SerialNumber = "JA00000002B" },
            new Display { SerialNumber = "JA00000003B" },
            new Display { SerialNumber = "JA00000004B" },
            new Display { SerialNumber = "JA00000005B" }
        };
        // ---------------------------------------------------------------

        private readonly DynamicCommandClient _client = new DynamicCommandClient(ApiServer, "new_client_id", "password", "admin@localhost", "password");

        [Test]
        public async Task GetUsersAsync()
        {
            //Get the configured Dynamic Solution users
            var response = await _client.GetApiAsync("api/users");
            response.EnsureSuccessStatusCode();

            //A List of users is returned
            var users = await response.Content.ReadAsAsync<List<User>>();
            foreach (User user in users)
            {
                TestContext.WriteLine(user.UserName);
            }
        }

        [Test]
        public async Task AddLocationAsync()
        {
            //Create new Location object 
            string locationGuid = Guid.NewGuid().ToString();
            Location newLocation = new Location
            {
                Name = "SampleLocation_" + locationGuid,
                TimeZone = TimeZoneInfo.Local.DisplayName,
                GeoLat = 51.4159438,
                GeoLong = -0.7402738,
                Comments = "This is a sample comment",
                //See API reference for details on which value to supply for RFRegionID
                RFRegionID = 1,
                FriendlyLocationName = "Sample Location " + locationGuid
            };

            //Post Location to API
            var response = await _client.PostApiAsync("api/locations", newLocation);
            response.EnsureSuccessStatusCode();
        }

        private async Task<List<Location>> GetLocations(string name)
        {
            //Search for locations using locationName. clientId is a legacy parameter that it always required for this call.
            var response = await _client.GetApiAsync($"api/locations?clientId=0&locationName={name}");
            response.EnsureSuccessStatusCode();
            //A list of locations will be returned.
            var locations = await response.Content.ReadAsAsync<List<Location>>();
            return locations;
        }

        [Test]
        public async Task UpdateLocationAsync()
        {
            //Ensure location exists and that we only get one result
            var locations = await GetLocations(existingLocation1.Name);
            Assert.AreEqual(1, locations.Count);

            //Update location
            Location updatedLocationDetails = new Location
            {
                Comments = "Updated Comment"
            };
            var response = await _client.PutApiAsync($"api/locations/name={existingLocation1.Name}", updatedLocationDetails);
            response.EnsureSuccessStatusCode();
        }

        [Test]
        public async Task GetLocationsAsync()
        {
            var locations = await GetLocations(existingLocation1.Name);

            //Make sure at least one location was found.
            Assert.AreNotEqual(0, locations.Count);
        }

        [Test]
        public async Task GetCommunicatorsAsync()
        {
            //Get all communicators.
            var response = await _client.GetApiAsync($"api/communicator");
            response.EnsureSuccessStatusCode();

            //A list of communicators is returned. Not all fields are filled in this call.
            var communicators = await response.Content.ReadAsAsync<List<Communicator>>();
        }

        [Test]
        public async Task GetDefaultCommunicatorFirmwareAsync()
        {
            await GetDefaultCommunicatorFirmware();
        }

        [Test]
        public async Task SetLocationDefaultFirmware()
        {
            var latestFirmware = await GetDefaultCommunicatorFirmware();
            var response = await _client.PostApiAsync($"api/locations/name={existingLocation1.Name}", latestFirmware);
            response.EnsureSuccessStatusCode();

        }

        public async Task<List<Communicator>> GetLocationCommunicators(Location location)
        {
            //Get all communicators at a specified location
            var response = await _client.GetApiAsync($"api/locations/name={existingLocation1.Name}/communicators");
            response.EnsureSuccessStatusCode();
            var communicators = await response.Content.ReadAsAsync<List<Communicator>>();
            return communicators;
        }

        public async Task<Communicator> GetCommunicator(string communicatorSerialNumber)
        {
            var response = await _client.GetApiAsync($"api/communicator/{communicatorSerialNumber}");
            response.EnsureSuccessStatusCode();
            var communicator = await response.Content.ReadAsAsync<Communicator>();
            return communicator;
        }

        public async Task<bool> IsLocationFirmwareOutOfDate(Location location)
        {
            //Firmware version is only available on a per communicator basis
            //So request a list of communicators at a location and then request further details on each one.
            var latestFirmware = await GetDefaultCommunicatorFirmware();
            var locationCommunicators = await GetLocationCommunicators(existingLocation1);
            foreach (Communicator communicator in locationCommunicators)
            {
                var detailedCommunicator = await GetCommunicator(communicator.SerialNumber);
                if (detailedCommunicator.FirmwareVersion != latestFirmware.Version)
                {
                    TestContext.WriteLine("Location firmware not up to date");
                    return true;
                }
            }
            TestContext.WriteLine("Location firmware is on latest firmware");
            return false;
        }

        [Test]
        public async Task UpdateLocationFirmware()
        {
            var latestFirmware = await GetDefaultCommunicatorFirmware();
            var response = await _client.PostApiAsync($"api/locations/name={existingLocation1.Name}/communicatorfirmware", latestFirmware);
            response.EnsureSuccessStatusCode();
            while (await IsLocationFirmwareOutOfDate(existingLocation1))
            {
                TestContext.WriteLine("Upgrading communicators. This could take up to 30 minutes...");
                TestContext.WriteLine("Waiting 30 seconds before checking again.");
                await Task.Delay(30000);
            }
        }

        private async Task<CommunicatorFirmware> GetDefaultCommunicatorFirmware()
        {
            var response = await _client.GetApiAsync($"api/locations/communicatorfirmware");
            response.EnsureSuccessStatusCode();

            //The default Dynamic Solution firmware is returned
            var communicatorFirmware = await response.Content.ReadAsAsync<CommunicatorFirmware>();
            return communicatorFirmware;
        }

        private async Task<Product> AddProduct()
        {
            Product newProduct = new Product
            {
                //ObjectID should be unique
                ObjectID = "Sample_" + Guid.NewGuid(),
                //Searchable values should be unique
                SearchableValues = new List<string>() { "Sample_" + Guid.NewGuid() },
                ObjectName = "Sample Product",
                ObjectDescription = "Some text about the sample product"
            };
            var response = await _client.PostApiAsync("api/objects", newProduct);
            response.EnsureSuccessStatusCode();
            return newProduct;
        }


        [Test]
        public async Task AddProductAsync()
        {
            //Add product
            Product newProduct = await AddProduct();
        }

        [Test]
        public async Task DeleteProductAsync()
        {
            //Add a product to delete.
            Product newProduct = await AddProduct();

            //Delete product
            var response = await _client.DeleteApiAsync($"api/objects/{newProduct.ObjectID}");
            response.EnsureSuccessStatusCode();
        }

        [Test]
        public async Task UpdateProductAsync()
        {
            //Add a product to update.
            Product newProduct = await AddProduct();

            //Update the product description.
            Product updatedProduct = new Product
            {
                ObjectDescription = "A new description.",
                SearchableValues = new List<string> { "Sample_" + Guid.NewGuid(), "Sample_" + Guid.NewGuid() }
            };
            var response = await _client.PutApiAsync($"api/objects/{newProduct.ObjectID}", updatedProduct);
            response.EnsureSuccessStatusCode();

        }

        [Test]
        public async Task SearchProductsAsync()
        {
            //Add a product to search for
            Product newProduct = await AddProduct();

            //Rquired search parameters. Search results are paginated so from/to values are required.
            int fromItem = 1;
            int toItem = 10;

            string objectDesc = "";
            string objectName = "Sample";

            //The legacy clientID and dataSourceId paramemters are always required with these values.
            var response = await _client.GetApiAsync($"api/objects/detail?clientId=0&dataSourceId=0&fromItem={fromItem}&toItem={toItem}&objName={objectName}&objDesc={objectDesc}");
            response.EnsureSuccessStatusCode();

            //Search returned a list contained within a ProductSearchResult object.
            var searchResponse = await response.Content.ReadAsAsync<ProductSearchResult>();

            //Loop though results.
            foreach (Product product in searchResponse.Objects)
            {
                TestContext.WriteLine($"Found: {product.ObjectName} with ID: {product.ObjectID}");
            }
        }

        [Test]
        public async Task GetProductAsync()
        {
            //Add a product to get
            Product newProduct = await AddProduct();

            //Get the product
            var response = await _client.GetApiAsync($"api/objects/{newProduct.ObjectID}");
            response.EnsureSuccessStatusCode();

            //Product object is returned
            var product = await response.Content.ReadAsAsync<Product>();
        }

        private async Task SendImage(string objectID, int page = 1, string locationName = null, string batchID = null)
        {
            //Create an image object. Read the image as bytes and convert to base64.
            Image image = new Image
            {
                ImageBase64 = Convert.ToBase64String(GetEmbeddedImageBytes("Chroma29_enquire_test_296x128.png")),
                ObjectID = objectID,
                //The type of display this image is meant for.
                //See Appendix A of the API reference for the full list.
                DisplayTypeID = 11,
                PageID = page,
                //The image type that is being supplied, either BMP, PBM or PNG. See API reference for details.
                ImageType = 3
            };

            //Send the image as a local override image
            if (locationName != null)
            {
                image.LocationName = locationName;
            }

            //Use a user supplied batchID
            //For more information on how to use this feature see section 5.1 of the System Management documentation.
            if (batchID != null)
            {
                image.UserDefinedBatchID = batchID;
            }
            var response = await _client.PostApiAsync($"api/objects/{image.ObjectID}/images", image);
            response.EnsureSuccessStatusCode();
        }

        private async Task SendImage(List<string> objectIDs, int page = 1, string locationName = null, string batchID = null)
        {
            //Create an image object. Read the image as bytes and convert to base64.
            MultiProductImage image = new MultiProductImage
            {
                ImageBase64 = Convert.ToBase64String(GetEmbeddedImageBytes("Chroma29_enquire_test_296x128.png")),
                ObjectIDs = objectIDs,
                //The type of display this image is meant for.
                //See Appendix A of the API reference for the full list.
                DisplayTypeID = 11,
                PageID = page,
                //The image type that is being supplied, either BMP, PBM or PNG. See API reference for details.
                ImageType = 3
            };

            //Send the image as a local override image
            if (locationName != null)
            {
                image.LocationName = locationName;
            }

            //Use a user supplied batchID
            //For more information on how to use this feature see section 5.1 of the System Management documentation.
            if (batchID != null)
            {
                image.UserDefinedBatchID = batchID;
            }
            var response = await _client.PostApiAsync($"api/objects/imagetomultipleobjects", image);
            response.EnsureSuccessStatusCode();
        }

        [Test]
        public async Task SendGlobalImageAsync()
        {
            //Add product to send image to
            Product newProduct = await AddProduct();
            //Send a Global image
            await SendImage(newProduct.ObjectID);
        }

        [Test]
        public async Task SendLocalImageAsync()
        {
            //Add product to send image to
            Product newProduct = await AddProduct();
            //Send a local override image to existingLocation1
            await SendImage(newProduct.ObjectID, page: 1, locationName: existingLocation1.Name);
        }

        [Test]
        public async Task SendMultiProductImageAsync()
        {
            //Add product to send image to
            Product newProduct1 = await AddProduct();
            Product newProduct2 = await AddProduct();
            //Send a Global image
            await SendImage(new List<string> { newProduct1.ObjectID, newProduct2.ObjectID });
        }
        [Test]
        public async Task ClearLocalImagesAsync()
        {
            //Send local override images
            Product newProduct = await AddProduct();
            await SendImage(newProduct.ObjectID, page: 1, locationName: existingLocation1.Name);
            await SendImage(newProduct.ObjectID, page: 2, locationName: existingLocation1.Name);

            //Provide a specification on which local overrides to clear

            var clearProductPagesSpec = new ClearProductPagesSpec {
                ClearObjectPages = new List<ClearObjectPage>
                {
                    new ClearObjectPage { ObjectIds = new List<string>{ newProduct.ObjectID }, Pages = new List<int>{ 1, 2 } },
                }
            };

            //Send the specification in the body
            var response = await _client.PostApiAsync($"api/objects/pages/clear?locationname={existingLocation1.Name}", clearProductPagesSpec);
            response.EnsureSuccessStatusCode();

            var clearResponse = await response.Content.ReadAsAsync<ClearProductPagesResponseList>();

            //A list of ClearProductPagesResponses which contain a list of products affected and a list of PageResults.
            //A PageResults object contains a page id and a boolean on whether or not a clear page was issued.  
            foreach (ClearProductPagesResponse clearProductPagesResponse in clearResponse.ClearObjectPagesResponse)
            {
                foreach (PageResult pageResult in clearProductPagesResponse.PageResults)
                {
                    if (pageResult.ClearIssued)
                    {
                        TestContext.WriteLine($"Page {pageResult.Page} on product(s) {String.Join(", ", clearProductPagesResponse.ObjectIds)} cleared");
                    }
                }
            }
        }

        [Test]
        public async Task ClearGlobalImagesAsync()
        {
            //Send global images
            Product newProduct = await AddProduct();
            await SendImage(newProduct.ObjectID);

            //Provide a specification on which global images to clear
            var clearProductPagesSpec = new ClearProductPagesSpec
            {
                ClearObjectPages = new List<ClearObjectPage>
                {
                    new ClearObjectPage { ObjectIds = new List<string>{ newProduct.ObjectID }, Pages = new List<int>{ 1, 2 } },
                }
            };

            //Send the specification in the body
            var response = await _client.PostApiAsync("api/objects/pages/clear", clearProductPagesSpec);
            response.EnsureSuccessStatusCode();
            var clearResponse = await response.Content.ReadAsAsync<ClearProductPagesResponseList>();

            //A list of  which ClearProductPagesResponses which contain a list of products affected and a list of PageResults.
            //A PageResults object contains a page id and a boolean on whether or not a clear page was issued.
            foreach (ClearProductPagesResponse clearProductPagesResponse in clearResponse.ClearObjectPagesResponse)
            {
                foreach (PageResult pageResult in clearProductPagesResponse.PageResults)
                {
                    if (pageResult.ClearIssued)
                    {
                        TestContext.WriteLine($"Page {pageResult.Page} on product(s) {String.Join(", ", clearProductPagesResponse.ObjectIds)} cleared");
                    }
                }
            }
        }

        private async Task<HttpResponseMessage> SendContentAsBatchRequest(MultipartContent content)
        {
            //Create the request to the batch service
            HttpRequestMessage batchRequest = new HttpRequestMessage(HttpMethod.Post, $"{ApiServer}/API/api/batch");
            //Associate the content with the message
            batchRequest.Content = content;

            //Need to pre-authenticate because the batch endpoint does not require authentication.
            HttpResponseMessage response = await _client.SendAsync(batchRequest, preAuthenticate: true);
            response.EnsureSuccessStatusCode();
            return response;
        }

        private static async Task<List<HttpResponseMessage>> ExtractResponsesFromBatch(HttpResponseMessage response)
        {
            //Reads the individual parts in the content and loads them in memory
            MultipartMemoryStreamProvider responseContents = await response.Content.ReadAsMultipartAsync();

            var responses = new List<HttpResponseMessage>();

            //Extracts each of the individual Http responses
            foreach (HttpContent individualContent in responseContents.Contents)
            {
                responses.Add(await individualContent.ReadAsHttpResponseMessageAsync());
            }
            return responses;
        }

        [Test]
        public async Task AddDisplaysAsync()
        {
            //A batch API endpoint can be used to send multiple requests together. This cuts down on excess HTTP traffic.
            //This add display batch request can be done as a normal PUT request if a batch request is not required.

            //Create the multipart/mixed message content
            MultipartContent content = new MultipartContent("mixed", "batch_" + Guid.NewGuid());
            foreach (Display display in displaysToUse)
            {
                //Create a message to add a display.
                //Create the different parts of the multipart content
                HttpMessageContent addDisplayContent = new HttpMessageContent(new HttpRequestMessage(HttpMethod.Put, $"{ApiServer}/API/api/locations/name={existingLocation1.Name}/displays/{display.SerialNumber}"));
                content.Add(addDisplayContent);
            }

            HttpResponseMessage response = await SendContentAsBatchRequest(content);
            List<HttpResponseMessage> responses = await ExtractResponsesFromBatch(response);
            foreach (HttpResponseMessage individualResponse in responses)
            {
                individualResponse.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task DeleteDisplaysAsync()
        {
            //A batch API endpoint can be used to send multiple requests together. This cuts down on excess HTTP traffic.
            //This delete display batch request can be done as a normal DELETE request if a batch request is not required.

            //Create the multipart/mixed message content
            MultipartContent content = new MultipartContent("mixed", "batch_" + Guid.NewGuid());
            foreach (Display display in displaysToUse)
            {
                //Create a message to add a display.
                //Create the different parts of the multipart content
                HttpMessageContent deleteDisplayContent = new HttpMessageContent(new HttpRequestMessage(HttpMethod.Delete, $"{ApiServer}/API/api/displays/{display.SerialNumber}"));
                content.Add(deleteDisplayContent);
            }

            HttpResponseMessage response = await SendContentAsBatchRequest(content);
            List<HttpResponseMessage> responses = await ExtractResponsesFromBatch(response);
            foreach (HttpResponseMessage individualResponse in responses)
            {
                individualResponse.EnsureSuccessStatusCode();
            }
        }

        [Test]
        public async Task SendImagesAsync()
        {
            //A batch API endpoint can be used to send multiple requests together. This cuts down on excess HTTP traffic.
            //This add display batch request can be done as a normal PUT request if a batch request is not required.

            //Create the multipart/mixed message content
            var batchID = "batch_" + Guid.NewGuid();
            MultipartContent content = new MultipartContent("mixed", batchID);
            var products = new List<Product>();
            for (int i = 0; i < 100; i++)
            {
                products.Add(await AddProduct());
            }

            foreach (Product product in products)
            {
                //Create an image object. Read the image as bytes and convert to base64.
                Image image = new Image
                {
                    ImageBase64 = Convert.ToBase64String(GetEmbeddedImageBytes("Chroma29_enquire_test_296x128.png")),
                    ObjectID = product.ObjectID,
                    //The type of display this image is meant for.
                    //See Appendix A of the API reference for the full list.
                    DisplayTypeID = 11,
                    PageID = 1,
                    //The image type that is being supplied, either BMP, PBM or PNG. See API reference for details.
                    ImageType = 3,
                    //Send the image as a local override image
                    LocationName = existingLocation1.Name,
                    //Use a user supplied batchID
                    //For more information on how to use this feature see section 5.1 of the System Management documentation.
                    UserDefinedBatchID = batchID
                };

                //Create a message to send an image.
                //Create the different parts of the multipart content
                HttpMessageContent sendImageContent = new HttpMessageContent(new HttpRequestMessage(HttpMethod.Post, $"{ApiServer}/API/api/objects/{image.ObjectID}/images"));
                sendImageContent.HttpRequestMessage.Content = new ObjectContent<Image>(image, new JsonMediaTypeFormatter());
                content.Add(sendImageContent);
            }

            HttpResponseMessage response = await SendContentAsBatchRequest(content);
            List<HttpResponseMessage> responses = await ExtractResponsesFromBatch(response);
            foreach (HttpResponseMessage individualResponse in responses)
            {
                individualResponse.EnsureSuccessStatusCode();
            }
        }


        private static byte[] GetEmbeddedImageBytes(string imageName)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (var resourceStream = assembly.GetManifestResourceStream($"CodeExamples.EmbeddedResources.{imageName}"))
            {
                var imageBytes = new byte[resourceStream.Length];
                var bytesRead = resourceStream.Read(imageBytes, 0, imageBytes.Length);
                if (bytesRead != imageBytes.Length)
                {
                    throw new Exception("Short read from resource stream");
                }
                return imageBytes;
            }
        }

        private async Task StoreImageBase64()
        {
            //Create an image object. Read the image as bytes and convert to base64.
            Image image = new Image
            {
                ImageBase64 = Convert.ToBase64String(GetEmbeddedImageBytes("Chroma29_enquire_test_296x128.png")),
                //The type of display this image is meant for.
                //See Appendix A of the API reference for the full list.
                DisplayTypeID = 11,
                //The image type that is being supplied, either BMP, PBM or PNG. See API reference for details.
                ImageType = 3
            };

            var response = await _client.PostApiAsync($"api/images/", image);
            response.EnsureSuccessStatusCode();
            var imageRef = await response.Content.ReadAsAsync<ImageRef>();
            Assert.That(imageRef.ImageReference, Does.StartWith("dd-imagestore:///"));
        }

        [Test]
        public async Task StoreImageBase64Async()
        {
            await StoreImageBase64();
        }

        [Test]
        public async Task StoreImageMultipartAsync()
        {
            await StoreImageMultipart();
        }

        private async Task StoreImageMultipart()
        {
            MultipartContent content = new MultipartContent("mixed");

            //Create an image object.
            Image image = new Image
            {
                //The type of display this image is meant for.
                //See Appendix A of the API reference for the full list.
                DisplayTypeID = 11,
                //The image type that is being supplied, either BMP, PBM or PNG. See API reference for details.
                ImageType = 3
            };
            //Create a message to send an image.
            //Create the different parts of the multipart content
            HttpContent sendImageContent = new ObjectContent<Image>(image, new JsonMediaTypeFormatter());
            content.Add(sendImageContent);

            //Read the image as bytes.
            byte[] imageBytes = GetEmbeddedImageBytes("Chroma29_enquire_test_296x128.png");
            HttpContent byteArrayContent = new ByteArrayContent(imageBytes);
            //In multi-type requests the ImageType must be specified. If the ImageType is also specified in the Image object this will overide it.
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(byteArrayContent);

            //Create the request
            HttpRequestMessage storeImageRequest = new HttpRequestMessage(HttpMethod.Post, $"{ApiServer}/API/api/images")
            {
                //Associate the content with the message
                Content = content
            };

            HttpResponseMessage response = await _client.SendAsync(storeImageRequest);
            response.EnsureSuccessStatusCode();

            var imageRef = await response.Content.ReadAsAsync<ImageRef>();
            Assert.That(imageRef.ImageReference, Does.StartWith("dd-imagestore:///"));
        }
    }
}