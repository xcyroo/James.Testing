﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using James.Testing.Rest.IntegrationTests.Models;
using Nancy;
using NUnit.Framework;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace James.Testing.Rest.IntegrationTests.RequestTests
{
    [TestFixture]
    public class GetTests : HostTestFixture
    {
        [Test]
        public void given_uri_for_existing_resource_when_getting_should_return_resource()
        {
            Request
                .Get<IList<Person>>(GetUriString(GetModule.PeopleResource))
                .Verify("Body != null", r => r.Body != null)
                .Verify("Count == 2", r => r.Body.Count == 2)
                .Verify("Item 1, FirstName = Todd", r => r.Body[0].FirstName == "Todd")
                .Verify("Item 1, LastName = Meinershagen", r => r.Body[0].LastName == "Meinershagen")
                .Verify("Item 2, FirstName = Brian", r => r.Body[1].FirstName == "Brian")
                .Verify("Item 2, LastName = Ellis", r => r.Body[1].LastName == "Ellis")
                .Verify("StatusCode == OK", r => r.StatusCode == HttpStatusCode.OK);
        }

        [Test]
        public void given_uri_for_non_existing_resource_when_getting_should_return_with_not_found_status()
        {
            Request
                .Get<object>(GetUriString("DoesNotExist"))
                .Verify("Body == null", r => r.Body == null)
                .VerifyThat(r => r.StatusCode).Is(HttpStatusCode.NotFound);
        }

        [Test]
        public void given_uri_for_existing_resource_with_headers_when_getting_should_return_resource_properly()
        {
            Request
                .Get<Dictionary<string, string>>(GetUriString(GetModule.HeadersResource),
                new {Id = "1;1", Test = "verify", x_medassets_auth = "test"})
                .VerifyThat(r => r.StatusCode).Is(HttpStatusCode.OK)
                .Verify(r => r.Body["id"] == "1;1")
                .Verify(r => r.Body["test"] == "verify")
                .Verify(r => r.Body["x-medassets-auth"] == "test")
                .Verify(r => r.Headers.GetValues("id").First() == "1;1")
                .Verify(r => r.Headers.GetValues("test").First() == "verify")
                .Verify(r => r.Headers.GetValues("x-medassets-auth").First() == "test");
        }

        [Test]
        public void given_uri_for_existing_resource_with_query_when_getting_should_return_resource_properly()
        {
            Request
                .Get<Person>(GetUriString(GetModule.QueryResource), null,
                    new {FirstName = "Todd", LastName = "Meinershagen"})
                .VerifyThat(r => r.StatusCode).Is(HttpStatusCode.OK)
                .Verify(r => r.Body.FirstName == "Todd")
                .Verify(r => r.Body.LastName == "Meinershagen");
        }

        [Test]
        public void given_uri_for_existing_resource_when_getting_as_dynamic_should_return_resource()
        {
            Request
                .GetAsDynamic(GetUriString(GetModule.DynamicResource))
                .Verify(x => x.Body.firstName == "Todd")
                .Verify(x => x.Body.lastName == "Meinershagen");
        }

        [Test]
        public void given_uri_for_existing_resource_with_headers_when_getting_as_dynamic_should_return_resource()
        {
            Request
                .GetAsDynamic(GetUriString(GetModule.HeadersResource),
                    new {Id = "1", FirstName = "Tammy", LastName = "Bennett"})
                .Verify(r => r.Body.id == "1")
                .Verify(r => r.Body.firstName == "Tammy")
                .Verify(r => r.Body.lastName == "Bennett")
                .Verify(r => r.Headers.GetValues("id").First() == "1")
                .Verify(r => r.Headers.GetValues("firstName").First() == "Tammy")
                .Verify(r => r.Headers.GetValues("lastName").First() == "Bennett");
        }

        [Test]
        public void given_uri_for_existing_resource_when_getting_as_bytes_should_return_resource()
        {
            Request
                .GetAsDynamic(GetUriString(GetModule.HeadersResource),
                    new { Id = "1", FirstName = "Tammy", LastName = "Bennett" })
                .Verify(r => r.Headers.GetValues("id").First() == "1")
                .Verify(r => r.Headers.GetValues("firstName").First() == "Tammy")
                .Verify(r => r.Headers.GetValues("lastName").First() == "Bennett");
        }
    }

    [TestFixture]
    public class given_existing_resource_when_getting_as_bytes : HostTestFixture
    {
        [TestFixtureSetUp]
        public void SetUp()
        {
            Request
                .GetAsBytes(GetUriString(GetModule.DocumentsResource) + "/" + Guid.NewGuid());
        }

        [Test]
        public void should_return_bytes()
        {
            Request
                .CurrentResponse<byte[]>()
                .Verify(r => r.Body.Length > 0);
        }

        [Test]
        public void should_be_ok_status()
        {
            Request
                .CurrentResponse<byte[]>()
                .Verify(r => r.StatusCode == HttpStatusCode.OK);
        }

        [Test]
        public void should_return_headers()
        {
            Request
                .CurrentResponse<byte[]>()
                .Verify(r => r.Headers.Any());
        }
    }

    public class GetModule : NancyModule
    {
        public const string PeopleResource = "People";
        public const string DocumentsResource = "Documents";
        public const string HeadersResource = "Headers";
        public const string DynamicResource = "Dynamic";
        public const string QueryResource = "Query";

        private readonly Person[] _people = new[]
        {
            new Person { Id = Guid.NewGuid(), FirstName = "Todd", LastName = "Meinershagen" },
            new Person { Id = Guid.NewGuid(), FirstName = "Brian", LastName = "Ellis" }
        };

        public GetModule()
        {
            Get[PeopleResource] = _ => Negotiate
                .WithModel(_people)
                .WithStatusCode(Nancy.HttpStatusCode.OK);

            Get[DynamicResource] = _ => Negotiate
                .WithModel(new {FirstName = "Todd", LastName = "Meinershagen"});

            Get[HeadersResource] = _ =>
            {
                var headers = Request.Headers.ToDictionary(header => header.Key, header => header.Value.First());

                return Negotiate
                    .WithModel(headers)
                    .WithHeaders(headers.Select(h => new {Header = h.Key, h.Value}).ToArray());
            };

            Get[QueryResource] = _ =>
            {
                var query = Request.Query;
                return Negotiate
                    .WithModel(new Person{FirstName = query.FirstName, LastName = query.LastName});
            };

            Get[DocumentsResource + "/{Id}"] = _ =>
            {
                var filePath = Path.Combine(Environment.CurrentDirectory, "SampleEstimate.pdf");
                return Response.FromByteArray(File.ReadAllBytes(filePath), "application/pdf");
            };
        }
    }
}
