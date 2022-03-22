const { URLSearchParams } = require('url')
const http = require('http')
const https = require('https')
const crypto = require('crypto')
const { spawn } = require('child_process')
const os = require('os')

nexarPage = (title, message) => `
    <html>
    <head>
      <link href="https://fonts.googleapis.com/css?family=Montserrat:400,700" rel="stylesheet" type="text/css">
      <title>${title}</title>
      <style>
        html {
          height: 100%;
          background-image: linear-gradient(to right, #000b24, #001440);
        }
        body {
          color: #ffffff;
        }
        .center {
          width: 100%;
          position: absolute;
          left: 50%;
          top: 50%;
          transform: translate(-50%, -50%);
          text-align: center;
        }
        .title {
          font-family: Montserrat, sans-serif;
          font-weight: 400;
        }
        .normal {
          font-family: Montserrat, sans-serif;
          font-weight: 300;
        }
      </style>
    </head>
    <body>
      <div class="center">
        <h1 class="title">${title}</h1>
        <p class="normal">${message}.</p>
      </div>
    </body>
    </html>
    `

REDIRECT_URI = 'http://localhost:3000/login';
AUTHORITY_URL = 'https://identity.nexar.com/connect/authorize';

scope = {
    'supply': 'supply.domain',
    'design': 'openid profile email design.domain user.access offline_access'
}

class NexarClient {
    #accessToken
    #id
    #secret
    #scope
    hostName = 'api.nexar.com'

    constructor(id, secret, scope = 'supply.domain') {
        this.#id = id
        this.#secret = secret
        this.#scope = scope
    }

    #getRequest(options, data) {
        return new Promise((resolve, reject) => {
            let req = https.request(options, res => {
                const contentType = res.headers['content-type'];
            
                let error;
                if (res.statusCode !== 200) {
                    error = new Error('Request Failed.\n' +
                        `Status Code: ${res.statusCode} ${res.statusMessage}`)
                } else if (!/^application\/json/.test(contentType)) {
                    error = new Error('Invalid content-type.\n' +
                        `Expected application/json but received ${contentType}`)
                }
                if (error) {
                    console.error(error.message)
                    // Consume response data to free up memory
                    res.resume()
                    return
                }
            
                let rawData = '';
                res.setEncoding('utf8');
                res.on('data', (chunk) => rawData += chunk );
                res.on('end', () => resolve(JSON.parse(rawData)));
            })
            req.on('error', (err) => reject(err));
            req.write(data)
            req.end()
        })
    }

    #launchBrowser(auth_request) {
        switch(os.platform()) {
            case 'win32':
                return spawn('start', ['""', `"${auth_request}"`], { 'shell': true })
            case 'darwin':
                return spawn('open', [`"${auth_request}"`], { 'shell': true })
            default:
                return spawn('xdg-open', [`"${auth_request}"`], { 'shell': true })
        }
    }

    #getUserAuthCode(id, code_challenge, scope) {      
        let auth_request = new URL(AUTHORITY_URL)
        const auth_params = new URLSearchParams({
            'response_type': 'code',
            'client_id': id,
            'redirect_uri': REDIRECT_URI,
            'scope': scope,
            'state': crypto.randomBytes(16).toString('hex'),
            'code_challenge': code_challenge,
            'code_challenge_method': 'S256'
        })
        auth_request.search = auth_params.toString()

        let server = http.createServer()
        server.listen(3000)
        let client;

        return new Promise((resolve, reject) => {
            server.on('request', (req, res) => {
                let url = new URL(req.url, `http://${req.headers.host}`)
                if (url.pathname == '/login') {
                    let error;
                    if (url.searchParams.get('state') != auth_params.get('state')) {
                        error = new Error('state information does not match')
                    } else if (!url.searchParams.has('code')) {
                        error = new Error('no code returned')
                    }
                    if (error) {
                        res.writeHead(400)
                        res.end(nexarPage('Authorization Failed!', error.message))
                        reject(error)
                    } else {
                        res.writeHead(200);
                        res.end(nexarPage('Welcome to Nexar', 'You can now return to the application.'))
                        server.close()
                        resolve(url.searchParams.get('code'))
                    }
                } else {
                    res.writeHead(404)
                    res.end()
                } 
            })

            client = this.#launchBrowser(auth_request.href)
        })

    }

    #getAccessTokenFromCode(id, secret, code_verifier, code) {
        const options = {
            hostname: 'identity.nexar.com',
            path: '/connect/token',
            method: 'POST',
            headers: {
                "Content-Type": "application/x-www-form-urlencoded"
            }
        }
        const data = new URLSearchParams({
            'grant_type': 'authorization_code',
            'client_id': id,
            'client_secret': secret,
            'code': code,
            'code_verifier': code_verifier,
            'redirect_uri': REDIRECT_URI
        })
        
        return this.#getRequest(options, data.toString())   
    }

    #getAccessToken(id, secret, scope) {
        if (scope == 'supply.domain') {        
            const options = {
                hostname: 'identity.nexar.com',
                path: '/connect/token',
                method: 'POST',
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded"
                }
            }
            const data = new URLSearchParams({
                'grant_type': 'client_credentials',
                'client_id': id,
                'client_secret': secret,        
                'scope': scope
            })

            return this.#getRequest(options, data.toString())
        }

        let urlSafe = (buffer) => 
            buffer.toString('base64')
            .replace(/\+/g, '-')
            .replace(/\//g, '_')
            .replace(/=/g, '')

        let pkceVerifier =  urlSafe(crypto.randomBytes(40))
        let pkceChallenge = urlSafe(crypto.createHash('sha256')
            .update(pkceVerifier)
            .digest())

        return this.#getUserAuthCode(id, pkceChallenge, scope)
            .then(code => { return this.#getAccessTokenFromCode(id, secret, pkceVerifier, code) })
    }

    #refreshToken(token) {
        if ('refresh_token' in token) {
            const options = {
                hostname: 'identity.nexar.com',
                path: '/connect/token',
                method: 'POST',
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded"
                }
            }
            const data = new URLSearchParams({
                'grant_type': 'refresh_token',
                'refresh_token': token.refresh_token,
                'client_id': this.#id,
                'client_secret': this.#secret,        
                'scope': this.#scope
            })

            this.#accessToken = this.#getRequest(options, data.toString())
        }

        this.#accessToken = this.#getAccessToken(this.#id, this.#secret, this.#scope)
    }

    #checkTokenExp(token) {
        let decodeJWT = (jwt) =>
            JSON.parse(
                Buffer.from(
                    jwt.split('.')[1]
                    .replace('-', '+')
                    .replace('_', '/'),
                    'base64'
                ).toString('binary')
            )

        let jwt = decodeJWT(token.access_token)
        let now = new Date()
        now.setMinutes(now.getMinutes() + 5)
    
        if ((jwt.exp * 1000) < now.getTime()) {
            //token is expired ... or will be in less than 5 minutes
            this.#refreshToken(token)
        }
    
        return this.#accessToken
    }
    
    query(gqlQuery, variables) {
        this.#accessToken = this.#accessToken || this.#getAccessToken(this.#id, this.#secret, this.#scope)

        return this.#accessToken
            .then(token => { return this.#checkTokenExp(token) })
            .then(token => {
                const options = {
                    hostname: this.hostName,
                    path: '/graphql',
                    method: 'POST',
                    headers: {
                        'Authorization': 'Bearer ' + token.access_token,
                        'Content-Type': 'application/json'
                    }
                }
                const data = {
                    'query': gqlQuery,
                    'variables': variables   
                }

                return this.#getRequest(options, JSON.stringify(data))
            })
    }

    async * pageGen(gqlQuery, gqlVariables, pageKey, pageSelect) {
        let pageInfo = {'hasNextPage': true}
        while (pageInfo.hasNextPage) {
            const response = await this.query(gqlQuery, gqlVariables)
            pageInfo = pageSelect(response.data).pageInfo
            gqlVariables[pageKey]= pageInfo.endCursor
            yield pageSelect(response.data).nodes
        }
    }
}

module.exports = {NexarClient, scope}