using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DynamicCommand;
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

        //Change for the AddDisplay and RemoveDisplay examples to use a different set of display serial numbers that you can add and remove displays to the system.
        //The existingLocation1 above will require a working communicator for example code to work.
        private readonly List<Display> displaysToUse = new List<Display> {
            new Display { SerialNumber = "JA00000001C" },
            new Display { SerialNumber = "JA00000002C" },
            new Display { SerialNumber = "JA00000003C" },
            new Display { SerialNumber = "JA00000004C" },
            new Display { SerialNumber = "JA00000005C" }
        };
        // ---------------------------------------------------------------

        //Make sure you have an additional API client created in your Dynamic Solution installation and put the client details here.
        //If you do not do this then you will get an error when authenticating.
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
            //Add a new location to Dynamic Solution.
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
            //Search for locations using locationName.
            var response = await _client.GetApiAsync($"api/locations?locationName={name}");
            response.EnsureSuccessStatusCode();
            //A list of locations will be returned.
            var locations = await response.Content.ReadAsAsync<List<Location>>();
            return locations;
        }

        [Test]
        public async Task UpdateLocationAsync()
        {
            //Ensure the location exists and that we only get one result
            var locations = await GetLocations(existingLocation1.Name);
            Assert.AreEqual(1, locations.Count);

            //Update the location by changing the location comment
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
            //Get a single location by name.
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
            //Get details about default Dynamic Communicator firmware version that will be used for newly created locations.
            await GetDefaultCommunicatorFirmware();
        }

        [Test]
        public async Task SetLocationDefaultFirmware()
        {
            //Change the Dynamic Communicator firmware in use for a location to the latest version.
            var latestFirmware = await GetDefaultCommunicatorFirmware();
            var response = await _client.PostApiAsync($"api/locations/name={existingLocation1.Name}/communicatorfirmware", latestFirmware);
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
            //Get details of a single Dynamic Communicator by its serial number
            var response = await _client.GetApiAsync($"api/communicator/{communicatorSerialNumber}");
            response.EnsureSuccessStatusCode();
            var communicator = await response.Content.ReadAsAsync<Communicator>();
            return communicator;
        }

        public async Task<bool> IsLocationFirmwareOutOfDate(Location location)
        {
            //Check a named location to see if it is running an out of date Dynamic Communicator firmware version.

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
            //Update all Dynamic Communicators at a named location to the latest available firmware version.
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
                SearchableValues = new List<string>() { $"Sample_{Guid.NewGuid()}", $"Sample_{Guid.NewGuid()}" },
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

        private async Task SendImage(List<string> objectIDs, int page = 1, string locationName = null, string batchID = null)
        {
           
            //Create an image object.
            MultiProductImage image = new MultiProductImage
            {
                ObjectIDs = objectIDs,
                //The type of display this image is meant for.
                //See Appendix A of the API reference for the full list.
                DisplayTypeName = "Chroma29",
                PageID = page,
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

            //Add the image data to the request
            MultipartContent content = new MultipartContent("related");
            content.Add(new ObjectContent<MultiProductImage>(image, new JsonMediaTypeFormatter()));
            var imagePartContent = new ByteArrayContent(GetEmbeddedImageBytes("Chroma29_enquire_test_296x128.png"));
            //The image type that is being supplied, either BMP, PBM or PNG. See API reference for details.
            imagePartContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(imagePartContent);
       
            //Finally send the request and check the result
            var request = new HttpRequestMessage(HttpMethod.Post, "api/objects/imagetomultipleobjects")
            {
                Content = content
            };

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        [Test]
        public async Task SendGlobalImageAsync()
        {
            //Add product to send image to
            Product newProduct = await AddProduct();
            //Send a Global image
            await SendImage(new List<string> { newProduct.ObjectID });
        }

        [Test]
        public async Task SendLocalImageAsync()
        {
            //Add product to send image to
            Product newProduct = await AddProduct();
            //Send a local override image to existingLocation1
            await SendImage(new List<string> { newProduct.ObjectID }, page: 1, locationName: existingLocation1.Name);
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
            await SendImage(new List<string> { newProduct.ObjectID }, page: 1, locationName: existingLocation1.Name);
            await SendImage(new List<string> { newProduct.ObjectID }, page: 2, locationName: existingLocation1.Name);

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
            await SendImage(new List<string> { newProduct.ObjectID });

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
            //Reads the individual response parts in the content and loads them in memory
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

            await SendBatchAndCheckReturnResponsesSuccessAsync(content);
        }


        [Test]
        public async Task GetDisplaysBatchAsync()
        {
            //A batch API endpoint can be used to send multiple requests together. This cuts down on excess HTTP traffic.
            //This Get display batch request can be done as a normal GET request if a batch request is not required.

            //Create the multipart/mixed message content
            MultipartContent content = new MultipartContent("mixed", "batch_" + Guid.NewGuid());
            foreach (Display display in displaysToUse)
            {
                //Create a message to get a display.
                //Create the different parts of the multipart content
                HttpMessageContent getDisplayContent = new HttpMessageContent(new HttpRequestMessage(HttpMethod.Get, $"{ApiServer}/API/api/displays/{display.SerialNumber}"));
                content.Add(getDisplayContent);
            }

            var responses = await SendBatchAndCheckReturnResponsesSuccessAsync(content);

            //Take the response list and read them into a display list
            List<GetDisplaysResponse> displays = new List<GetDisplaysResponse> { };
            foreach (HttpResponseMessage individualResponse in responses)
            {
                GetDisplaysResponse displayDetails = await individualResponse.Content.ReadAsAsync<GetDisplaysResponse>();

                displays.Add(displayDetails);
            }
            
            //Check the results
            Assert.AreEqual(displaysToUse.Count(), displays.Count());

            for (int index = 0;  index < displaysToUse.Count; index++)
            {
                Assert.AreEqual(displaysToUse[index].SerialNumber, displays[index].SerialNumber);
            }
        }


        private async Task<List<HttpResponseMessage>> SendBatchAndCheckReturnResponsesSuccessAsync(MultipartContent content)
        {
            HttpResponseMessage response = await SendContentAsBatchRequest(content);
            List<HttpResponseMessage> responses = await ExtractResponsesFromBatch(response);
            foreach (HttpResponseMessage individualResponse in responses)
            {
                individualResponse.EnsureSuccessStatusCode();
            }
            return responses;
        }


        [Test]
        public async Task RemoveDisplaysAsync()
        {
            //A batch API endpoint can be used to send multiple requests together. This cuts down on excess HTTP traffic.
            //This remove display batch request can be done as a normal DELETE request if a batch request is not required.

            //Create the multipart/mixed message content
            MultipartContent content = new MultipartContent("mixed", "batch_" + Guid.NewGuid());
            foreach (Display display in displaysToUse)
            {
                //Create a message to add a display.
                //Create the different parts of the multipart content
                HttpMessageContent removeDisplayContent = new HttpMessageContent(new HttpRequestMessage(HttpMethod.Delete, $"{ApiServer}/API/api/displays/{display.SerialNumber}"));
                content.Add(removeDisplayContent);
            }

            await SendBatchAndCheckReturnResponsesSuccessAsync(content);
        }

        [Test]
        public async Task SendImagesAsync()
        {
            //A batch API endpoint can be used to send multiple requests together. This cuts down on excess HTTP traffic.

            //  Create the products we are going to assign the image to
            var products = new List<Product>();
            for (int i = 0; i < 100; i++)
            {
                products.Add(await AddProduct());
            }

            //Create the content body for the batch request
            var batchID = "batch_" + Guid.NewGuid();
            MultipartContent content = new MultipartContent("mixed", batchID);

            //  Create the request for each image send that the batch is going to contain.
            foreach (Product product in products)
            {
                //Create an image object.
                MultiProductImage image = new MultiProductImage
                {
                    ObjectIDs = new List<string> { product.ObjectID },
                    //The type of display this image is meant for.
                    //See Appendix A of the API reference for the full list.
                    DisplayTypeName = "Chroma29",
                    PageID = 1,
                    //Send the image as a local override image
                    LocationName = existingLocation1.Name,
                    //Use a user supplied batchID
                    //For more information on how to use this feature see section 5.1 of the System Management documentation.
                    UserDefinedBatchID = batchID
                };

                //Create a message to send an image.
                //Create the different parts of the multipart content
                MultipartContent imageContent = new MultipartContent("related")
                {
                    new ObjectContent<MultiProductImage>(image, new JsonMediaTypeFormatter())
                };
                var imagePartContent = new ByteArrayContent(GetEmbeddedImageBytes("Chroma29_enquire_test_296x128.png"));
                imagePartContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                imageContent.Add(imagePartContent);

                HttpMessageContent sendImageContent = new HttpMessageContent(new HttpRequestMessage(HttpMethod.Post, $"{ApiServer}/API/api/objects/imagetomultipleobjects"));
                sendImageContent.HttpRequestMessage.Content = imageContent;
                content.Add(sendImageContent);
            }

            //  Now send all the image assigns as a single batch request.
            await SendBatchAndCheckReturnResponsesSuccessAsync(content);
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

        [Test]
        public async Task StoreImageUsingMultipartMixedAsync()
        {
            await StoreImageUsingMultipartMixed();
        }

        [Test]
        public async Task StoreImageUsingMultipartRelatedAsync()
        {
            await StoreImageUsingMultipartRelated();
        }

        [Test]
        public async Task StoreImageUsingMultipartFormDataAsync()
        {
            await StoreImageUsingMultipartFormData();
        }

        [Test]
        public async Task StoreImageAndThenSendAsync()
        {
            var imageRef = await StoreImageUsingMultipartMixed();
            Product newProduct = await AddProduct();
            await SendImageUsingReferenceAsync(new List<string> { newProduct.ObjectID }, imageRef);
        }

        [Test]
        public async Task StoreImageAndThenBatchSendAsync()
        {
            var imageRef = await StoreImageUsingMultipartRelated();
            await SendImageBatchUsingReferenceAsync(imageRef);
        }

        private async Task<ImageRef> StoreImageUsingMultipartMixed()
        {
            return await StoreImageUsingMultipartRequest("mixed");
        }

        private async Task<ImageRef> StoreImageUsingMultipartRelated()
        {
            return await StoreImageUsingMultipartRequest("related");
        }

        private async Task<ImageRef> StoreImageUsingMultipartRequest(string multipartType)
        {
            //Create an image object.
            Image image = new Image
            {
                //The type of display this image is meant for.
                //See Appendix A of the API reference for the full list.
                DisplayTypeName = "Chroma29",
            };

            MultipartContent content = new MultipartContent(multipartType);
            content.Add(new ObjectContent<Image>(image, new JsonMediaTypeFormatter()));
            var imagePartContent = new ByteArrayContent(GetEmbeddedImageBytes("Chroma29_enquire_test_296x128.png"));
            //The image type that is being supplied, either BMP, PBM or PNG. See API reference for details.
            imagePartContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(imagePartContent);

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
            return imageRef;
        }

        private async Task<ImageRef> StoreImageUsingMultipartFormData()
        {
            //Create an image object.
            Image image = new Image
            {
                //The type of display this image is meant for.
                //See Appendix A of the API reference for the full list.
                DisplayTypeName = "Chroma29",
            };

            //  Must set the content disposition name appropriately to allow the API to recognise which part is the data and
            //  which part is the image bytes.
            MultipartContent content = new MultipartContent("form-data");
            var objectPartContent = new ObjectContent<Image>(image, new JsonMediaTypeFormatter());
            objectPartContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline") { Name = "data" };
            content.Add(objectPartContent);
            var imagePartContent = new ByteArrayContent(GetEmbeddedImageBytes("Chroma29_enquire_test_296x128.png"));
            //The image type that is being supplied, either BMP, PBM or PNG. See API reference for details.
            imagePartContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            imagePartContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { Name = "image" };
            content.Add(imagePartContent);

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
            return imageRef;
        }

        private async Task SendImageUsingReferenceAsync(List<string> objectIDs, ImageRef imageRef, int page = 1, string locationName = null, string batchID = null)
        {
            //Create an image object.
            MultiProductImage image = new MultiProductImage
            {
                ImageReference = imageRef.ImageReference,
                ObjectIDs = objectIDs,
                //The type of display this image is meant for.
                //See Appendix A of the API reference for the full list.
                DisplayTypeName = "Chroma29",
                PageID = page,
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

            var response = await _client.PostApiAsync<MultiProductImage>("api/objects/imagetomultipleobjects", image);
            response.EnsureSuccessStatusCode();
        }

        private async Task SendImageBatchUsingReferenceAsync(ImageRef imageRef)
        {
            //A batch API endpoint can be used to send multiple requests together. This cuts down on excess HTTP traffic.

            //  Create the products we are going to assign the image to
            var products = new List<Product>();
            for (int i = 0; i < 100; i++)
            {
                products.Add(await AddProduct());
            }

            //Create the content body for the batch request
            var batchID = "batch_" + Guid.NewGuid();
            MultipartContent content = new MultipartContent("mixed", batchID);

            foreach (Product product in products)
            {
                //  Create the request for each image send that the batch is going to contain.
                MultiProductImage image = new MultiProductImage
                {
                    ImageReference = imageRef.ImageReference,
                    ObjectIDs = new List<string> { product.ObjectID },
                    //The type of display this image is meant for.
                    //See Appendix A of the API reference for the full list.
                    DisplayTypeName = "Chroma29",
                    PageID = 1,
                    //Send the image as a local override image
                    LocationName = existingLocation1.Name,
                    //Use a user supplied batchID
                    //For more information on how to use this feature see section 5.1 of the System Management documentation.
                    UserDefinedBatchID = batchID
                };

                //Create a message to send an image.
                //Create the different parts of the multipart content
                HttpMessageContent sendImageContent = new HttpMessageContent(new HttpRequestMessage(HttpMethod.Post, $"{ApiServer}/API/api/objects/imagetomultipleobjects"));
                sendImageContent.HttpRequestMessage.Content = new ObjectContent<MultiProductImage>(image, new JsonMediaTypeFormatter());
                content.Add(sendImageContent);
            }

            //  Now send all the image assigns as a single batch request.
            await SendBatchAndCheckReturnResponsesSuccessAsync(content);
        }

        [Test]
        public async Task UnassignProductsFromDisplayAsync()
        {
            //Get the display to use: this assumes that you have already added the display.
            //You can use AddDisplaysAsync()
            var display = displaysToUse.First();

            //Add a product to use
            Product product = await AddProduct();

            //Assigning the product to display
            await AssignProduct(product.ObjectID,display.SerialNumber);

            //Unassign all products from the display
            var response = await _client.PostApiAsync($"api/displays/{display.SerialNumber}/objects/remove/", true);
            response.EnsureSuccessStatusCode();
        }

        private async Task AssignProduct(string objectID, string serialNumber)
        {
            //Creating a list of products
            List<ObjectSequence> products = new List<ObjectSequence>()
            {
                //Add products to list
                new ObjectSequence (){ ObjectId = objectID, Sequence = 1}
            };

            //Assigning product to display
            var response = await _client.PostApiAsync($"api/displays/{serialNumber}/objects", products);
            response.EnsureSuccessStatusCode();
        }

        [Test]
        public async Task AssignProductToDisplayAsync()
        {
            //Add product to use
            var product = await AddProduct();

            //Constructing a list of products to pass to the API
            List<ObjectSequence> products = new List<ObjectSequence>()
            {
                new ObjectSequence (){ ObjectId = product.ObjectID, Sequence = 1}
            };  
           
            //Get the display to use: this assumes that you have already added the display.
            //You can use AddDisplaysAsync()
            var display = displaysToUse.First();
            
            //Assign the product to the display
            var response = await _client.PostApiAsync($"api/displays/{display.SerialNumber}/objects", products);
            response.EnsureSuccessStatusCode();
        }

        [Test]
        public async Task AssignMultipleProductsToDisplayAsync()
        {
            //Add products to use
            var product1 = await AddProduct();
            var product2 = await AddProduct();

            //Constructing a list of products to pass to the API
            List<ObjectSequence> products = new List<ObjectSequence>()
            {
                new ObjectSequence (){ ObjectId = product1.ObjectID, Sequence = 1},
                new ObjectSequence (){ ObjectId = product2.ObjectID, Sequence = 2}
            };
        }


        [Test]
        public async Task AssignMultipleProductsToDisplayBySearchValueAsync()
        {
            //Add products to use
            var product1 = await AddProduct();
            var product2 = await AddProduct();

            //Constructing a list of search values to pass to the API
            List<SearchValueObject> searchValues = new List<SearchValueObject>()
            {
                new SearchValueObject (){ SearchValue = product1.SearchableValues.First(), Sequence = 1},
                new SearchValueObject (){ SearchValue = product2.SearchableValues.Last(), Sequence = 2}
            };

            //Get the display to use: this assumes that you have already added the display.
            //You can use AddDisplaysAsync()
            var display = displaysToUse.First();

            //Assign multiple products to the display using searchable values
            var response = await _client.PostApiAsync($"api/displays/{display.SerialNumber}/searchablevalues", new SearchValuesObject() { SearchValues = searchValues});
            response.EnsureSuccessStatusCode();
        }
        [Test]
        public async Task AssignProductToDisplayBySearchValueAsync()
        {
            //Add products to use
            var product = await AddProduct();

            //Constructing a list of search values to pass to the API
            List<SearchValueObject> searchValues = new List<SearchValueObject>()
            {
                new SearchValueObject (){ SearchValue = product.SearchableValues.First(), Sequence = 1}
            };

            //Get the display to use: this assumes that you have already added the display.
            //You can use AddDisplaysAsync()
            var display = displaysToUse.First();

            //Assign multiple products to the display using searchable values
            var response = await _client.PostApiAsync($"api/displays/{display.SerialNumber}/searchablevalues", new SearchValuesObject() { SearchValues = searchValues });
            response.EnsureSuccessStatusCode();
        }
    }
}