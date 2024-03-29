using Xunit;
using ScrapperLibrary.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System;

namespace ScrapperUnitTests
{
    public class FileServiceTests
    {
        [Fact]
        public void WriteAndReadFile()
        {
            ILogger<FileService> logger = Mock.Of<ILogger<FileService>>();

            FileService fileService = new(logger);

            List<string> lines = new();

            Random random = new();
            int number = random.Next(100);
            lines.Add($"lineoftest: {number}");

            fileService.WriteFile("files/debug", "unittest.txt", lines, true);

            List<string> resultList = fileService.ReadFile("files/debug", "unittest.txt");
            
            bool result = false;

            if(resultList.Count == 1 && resultList[0] == $"lineoftest: {number}")
            {
                result = true;
            }

            Assert.True(result, "Created file succesfully");
        }
    }
}