# DiffB64

## Introduction:
This project provides a web API to compare two Base64 encoded strings. 

## How to Use:
Use Http Put method to put 2 http endpoints (<host>/v1/diff//left and <host>/v1/diff//right) that accept 
JSON containing base64 encoded binary data <br>
Use Http Get method to get the diff-ed results of the two inputs on a third endpoint (<host>/v1/diff/). The diff-ed results has the following JSON format:

If two binaries are equal return:
```javascript
{
  "diffResultType": "Equals"
}
If two binaries have different size return:
{
  "diffResultType": "SizeDoNotMatch"
}
```

If two binaries have same size, return the offsets and length of the diff:
```javascript
{
  "diffResultType": "ContentDoNotMatch",
  "diffs": [
    {
      "offset": 0,
      "length": 1
    },
    {
      "offset": 2,
      "length": 2
    }
  ]
}
```
## Example:

| End-point | Request | Response |
| ----------| --------|----------|
| 1 | GET /v1/diff/1 | 	404 Not Found |
| 2 | PUT /v1/diff/1/left <pre>{<br>  "data": "AAAAAA=="<br>}</pre>| 201 Created |
| 3 | GET /v1/diff/1 | 404 Not Found |
| 4 | PUT /v1/diff/1/right <pre>{<br>  "data": "AAAAAA=="<br>}</pre>| 201 Created |
| 5 | GET /v1/diff/1 | 200 OK <pre>{<br> "diffResultType": "Equals"<br>}</pre> | 
| 6 | PUT /v1/diff/1/right <pre>{<br>  "data": "AQABAQ=="<br>}</pre>| 201 Created |
| 7 | GET /v1/diff/1 | 200 OK <pre>{<br> "diffResultType": "ContentDoNotMatch", <br>  "diffs": [ <br>    { <br>      "offset": 0, <br>      "length": 1 <br>    }, <br>    { <br>      "offset": 2, <br>      "length": 2 <br>    } <br>  ] <br>}</pre> |
| 8 | PUT /v1/diff/1/left <pre>{<br>  "data": "AAA="<br>}</pre>|201 Created |
| 9 | GET /v1/diff/1 | 200 OK <pre>{<br>  "diffResultType": "SizeDoNotMatch"<br>}</pre> |

## Requirement:
* This application is based on ASP.NET WebAPI framework. It is developed and tested on .NET 4.6.1
* Install Visual Studio preferably VS2017
* Install Docker Engine for Windows

## Build
* From Visual Studio 2017 Import solution "DiffB64"
* Build Solution

## Deployment
1. From Visual Studio 2017
```
Start without Debugging (Ctrl+F5)
```
This will choose a random private local ip address running the Docker image
The IP address can be found in output window.

2. From command line
```
cd <repo>/DiffB64 (Go to DiffB64 project directory which contains "DockerFile")
docker build -t <appname> .
docker run -d --name <container_name> --rm <appname>
docker ps (Check the <appname> should run in <container_name>)
docker inspect --format="{{range .NetworkSettings.Networks}}{{.IPAddress}} {{end}}" <container_name>
This will indicate the current IP address used by docker for the application
```
## Test
1. The project contains DiffB64.Test project for UnitTest and IntegrationTest. Those can be run standalone from TestExplore in Visual Studio
2. It's also possible to test it as a black box by using tools such as wget or fiddler. Use IP address from the deployment step 
3. An example of a running instance of the project is hosted on http://diffb64.azurewebsites.net/
You can play around it with Wget
```
wget -q -O - --method=PUT --load-cookies cookies.txt --save-cookies cookies.txt --keep-session-cookies --header='Content-Type: application/json' --body-data='{ "data": "AAAAAA==" }' http://diffb64.azurewebsites.net/v1/diff/2/left

wget -q -O - --method=PUT --load-cookies cookies.txt --save-cookies cookies.txt --keep-session-cookies --header='Content-Type: application/json' --body-data='{ "data": "AAAAAA==" }' http://diffb64.azurewebsites.net/v1/diff/2/right

wget -q -O - --load-cookies cookies.txt --save-cookies cookies.txt --keep-session-cookies http://diffb64.azurewebsites.net/v1/diff/2

```
Have Fun :)
