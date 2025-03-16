# Nager.MtaMilter

## Sources:
- https://github.com/emersion/go-milter/blob/master/milter-protocol.txt
- https://gitlab.com/noumenia/libmilterphp/-/blob/master/library/Milter.inc.php?ref_type=heads

## Tested with Stalwart Mail Server

https://stalw.art/docs/install/docker/

**Docker Run Stalwart Mail Server**
```
docker run -d -ti -p 443:443 -p 8080:8080 -p 25:25 -p 587:587 -p 465:465 -p 143:143 -p 993:993 -p 4190:4190 -p 110:110 -p 995:995 -v c:\Temp\stalwart:/opt/stalwart-mail --name stalwart-mail stalwartlabs/mail-server:latest
```

**Example Mail**
```
220 a95ce8492965 Stalwart ESMTP at your service
helo test.de
250 a95ce8492965 you had me at HELO
mail from:<sender@test.de>
250 2.1.0 OK
rcpt to:<test@test.de>
250 2.1.5 OK
data
354 Start mail input; end with <CRLF>.<CRLF>
Date: Sun, 09 Mar 2025 06:28:30 -0700
From: Max <max@test.com>
To: "Muster" <muster@test.de>
Subject: Test Mail


test mail

.
```


**Example Configuration**
![Stalwart](https://github.com/user-attachments/assets/8a624f35-7883-42b2-947f-d13efa942004)
