/* Required modules */
const crypto = require('crypto'); //JWT
const net = require('net'); // Server Authentication (TCP)
const http = require('http'); // BBB RESTful API 
const path = require('path'); // BBB RESTful API
const b = require('bonescript'); // Gives access to the BBB hardware.

/* BBB GPIO Variables */
const TEMPSENSOR = "P9_40"; // Only change this for YOUR temperature sensing circuit PIN
const HEATSOURCE = "PX_XX"; // Only change this for YOUR heat ON/OFF generating circuit PIN

/* Networking Variables */
const HOST = '192.168.137.50'; // BBB IPv4
// const PORT = 49490;
const PORT = 15558;
/* Server Variables */
var forceHeat = false;
var temp = "-1";
var rawtemp = "-1";
var status = "OFF";
var authtoken = "-1";
var mysensorID = "sensor_60"; // X = lab group number
var secretkey = "password6"; // X = lab group number
var authuname = "admin6"; // X = lab group number
var authpword = "password6"; // X = lab group number
var count = 0;

/* Network Security Variables*/
const key = "cpe4020lab10";
const algorithm = 'aes256';
const inputEncoding = 'utf8';
const outputEncoding = 'hex';

function base64url(x) {

    // Remove padding equal characters
    x = x.replace(/=+$/, '');
    
    // Replace characters according to base64url specifications
    x = x.replace(/\+/g, '-');
    x = x.replace(/\//g, '_');
    
    return x;
}

/* Creates a web-server and facilitates API calls */
const server = http.createServer((req, res) => {
    if (req.url === path.normalize('/')) {
    
        /* Not authorized to view the root resource. */
        res.writeHead(403, { 'Content-Type': 'text/plain' });
        res.end("You do not have permission to view this resource.");
    
    } else if (req.url === path.normalize('/api/temp')) {
        readTemp();
        count++;
        console.log("Sent: " + count);
        res.end("tempID:"+mysensorID +",status:"+status+",temp:"+temp+",units:F");
    
    } else if (req.url === path.normalize('/api/temp/json')) {
        readTemp();
        var myObj = null;
        if(authtoken=="-1")
        {
            myObj = {authtoken: authtoken, tempID: mysensorID, temp: rawtemp, status: status.toString(), username: authuname, password: authpword};
        }
        else
        {
            myObj = {authtoken: authtoken, tempID: mysensorID, temp: temp.toString(), status: status.toString()};
        }
        
        res.end(JSON.stringify(myObj));
    
    } else if ((req.url.indexOf('?') > -1)) {
    
        var outpost = "BAD";
        var sarr = req.url.split('?');
        if(sarr[0] == path.normalize('/api/temp/postToken'))
        {
            var sarr2 = sarr[1].split('=');
            if(sarr2[0]=='atoken')
            {
                if(sarr2[1].length == 4)
                {
                    authtoken = sarr2[1];
                    outpost= "OK";
                }
            }
        }
        res.end(outpost);
    
    } else if (req.url === path.normalize('/api/temp/jwt')) {
        readTemp();
        let headerStr = base64url(new Buffer(JSON.stringify({alg: "HS256", typ: "JWT"})).toString("base64"));
        //console.log("HEADER: " + headerStr);
        
        var myObj = null;
        if(authtoken=="-1")
        {
            myObj = {authtoken: authtoken, tempID: mysensorID, temp: temp.toString(), status: status.toString(), username: authuname, password: authpword};
           //var encObj = myObj.encrypt("sha256", "objKey");
        }
        else
        {
            myObj = {authtoken: authtoken, tempID: mysensorID, temp: temp.toString(), status: status.toString()};
        }
        
        //var payloadStr = base64url(new Buffer(JSON.stringify(encObj)).toString("base64"));

        
        var payloadStr = base64url(new Buffer(JSON.stringify(myObj)).toString("base64"));
        //console.log("PAYLOAD: " + payloadStr);
                   
        // var encPayload = payloadStr.encrypt("sha256", "objKey");
        // var theToken = headerStr + "." + encPayload;

        
        var theToken = headerStr + "." + payloadStr;
        //console.log("Token: " + theToken);
        
        
        
        var hmac = crypto.createHmac('sha256', secretkey);
        hmac.update(theToken);
        
        var signStr = hmac.digest('base64');
        
        //var encSignstr = signStr.encrypt("sha256", "objKey")
        //var theFullJWT = theToken + "." + base64url(encSignstr);

        
        var theFullJWT = theToken + "." + base64url(signStr);
        //console.log("SIGNATURE: " + base64url(signStr));
        //console.log("JWT: " + theFullJWT);
        
        
        
        res.end(theFullJWT); 
    
    } else if (req.url === path.normalize('/api/temp/resetToken')) {
    
        authtoken = "-1";
        res.end("OK");
    
    } else {
    
        /* Page not found */
        res.writeHead(404, { 'Content-Type': 'text/plain' });
        res.end("this page doesn't exist");
    
    }
});



function startTemp(){
    setInterval(readTemp, 1000);
    console.log("working!");
}

function readTemp(){
    b.analogRead("P9_40", printStatus);
}

function printStatus(x) {
    var analogVoltage = x.value*1800; //ADC Value converted to voltage
    var tempC = (analogVoltage - 500) / 10;
    var tempF = (tempC * 9/5) + 32;
    temp = tempF.toFixed(2).toString();
    // Write a message to the socket as soon as the client is connected, the server will receive it as message from the client 
    
//client connect send tempF.toString()

    //client connect send tempF.toString()
    
    console.log("temp c=" + tempC + " Temp F=" + tempF);
  
}

function encrypt(value, useKey = key) {
    var cipher = crypto.createCipher(algorithm, key);
    var encrypted = cipher.update(value, inputEncoding, outputEncoding);
    encrypted += cipher.final(outputEncoding)
    return encrypted
}

startTemp();

/* Start the http server. Non-blocking. */
server.listen(PORT,HOST);
console.log('Web-Server Listening...');

