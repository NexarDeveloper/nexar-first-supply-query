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
const TOKEN_OPTIONS = {
    hostname: 'identity.nexar.com',
    path: '/connect/token',
    method: 'POST',
    headers: {
        "Content-Type": "application/x-www-form-urlencoded"
    }
}
const PORT = 3000
const REDIRECT_URI = `http://localhost:${PORT}/login`;
const AUTHORITY_URL = 'https://identity.nexar.com/connect/authorize';

function launchBrowser(auth_request) {
    switch(os.platform()) {
        case 'win32':
            return spawn('start', ['""', `"${auth_request}"`], { 'shell': true })
        case 'darwin':
            return spawn('open', [`"${auth_request}"`], { 'shell': true })
        default:
            return spawn('xdg-open', [`"${auth_request}"`], { 'shell': true })
    }
}

function decodeJWT(jwt) {
    return JSON.parse(
        Buffer.from(
            jwt.split('.')[1]
            .replace('-', '+')
            .replace('_', '/'),
            'base64'
        ).toString('binary')
    )
}

function getRequest(options, data) {
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

class NexarClient {
    #accessToken
    #exp
    #id
    #secret
    #scope
    hostName = 'api.nexar.com'
    static scopes = {
        'supply': 'supply.domain',
        'design': 'openid profile email design.domain user.access offline_access'
    }
        
    /**
     * Client for the Nexar API to manage authorization and requests.
     * @param {string} id - the client id assigned to a Nexar application.
     * @param {string} secret - the client secret assigned to a Nexar application.
     * @param {string} [scope] - the resources required for authorization
     */

    constructor(id, secret, scope = NexarClient.scopes.supply) {
        this.#id = id
        this.#secret = secret
        this.#scope = scope
    }

    set host(name) { this.hostName = name }

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

        let client;
        let server = http.createServer()
        server.listen(PORT)

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

            client = launchBrowser(auth_request.href)
        })
    }

    #getAccessTokenFromCode(id, secret, code_verifier, code) {
        const data = new URLSearchParams({
            'grant_type': 'authorization_code',
            'client_id': id,
            'client_secret': secret,
            'code': code,
            'code_verifier': code_verifier,
            'redirect_uri': REDIRECT_URI
        })
        
        return getRequest(TOKEN_OPTIONS, data.toString())   
    }

    #getAccessToken(id, secret, scope) {
        if (scope == 'supply.domain') {        
            const data = new URLSearchParams({
                'grant_type': 'client_credentials',
                'client_id': id,
                'client_secret': secret,        
                'scope': scope
            })

            return getRequest(TOKEN_OPTIONS, data.toString())
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
            const data = new URLSearchParams({
                'grant_type': 'refresh_token',
                'refresh_token': token.refresh_token,
                'client_id': this.#id,
                'client_secret': this.#secret,        
                'scope': this.#scope
            })

            return getRequest(TOKEN_OPTIONS, data.toString())
        }

        return this.#getAccessToken(this.#id, this.#secret, this.#scope)
    }

    #checkTokenExp() {
        this.#exp = this.#exp ||
            this.#accessToken.then(token => { return decodeJWT(token.access_token)?.exp })

        return this.#exp
            .then(exp => {
                let now = new Date()
            
                if ((exp * 1000) < now.setMinutes(now.getMinutes() + 5)) {
                    //token is expired ... or will be in less than 5 minutes
                    this.#exp = undefined
                    return this.#accessToken
                        .then(token => {return this.#refreshToken(token)})
                }

                return this.#accessToken
            })
    }
    
    /**
     * Make a request to the Nexar API
     * @param {string} gqlQuery - graphQL string containing the query/mutation.
     * @param {object} variables - key/value pairs for variables used in the gqlQuery.
     * @returns {object} - The Nexar API response
     */

     query(gqlQuery, variables) {
        this.#accessToken = this.#accessToken || this.#getAccessToken(this.#id, this.#secret, this.#scope)

        return this.#checkTokenExp()
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

                return getRequest(options, JSON.stringify(data))
            })
    }

    /**
     * Iterable for a graphQL Type implementing a node interface.
     * NB: the query must include a variable to set the the cursor and the pageInfo field on the Type.
     * @async
     * @generator
     * @param {string} pageKey - graphQL variable name for setting the cursor: desProjects(after: $pageKey).
     * @param {function} pageSelect - return from data response the type with node interface: (data) => data.desProjects
     * @yields {object} - a page of the graphQL Type implementing a node interface
     */

    async * pageGen(gqlQuery, gqlVariables, pageKey, pageSelect) {
        let pageInfo = {'hasNextPage': true}
        while (pageInfo.hasNextPage) {
            const response = await this.query(gqlQuery, gqlVariables)

            pageInfo = pageSelect(response.data).pageInfo
            gqlVariables[pageKey] = pageInfo.endCursor

            yield pageSelect(response.data).nodes
        }
    }
}

module.exports = {NexarClient}