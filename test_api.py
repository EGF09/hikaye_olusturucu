import urllib.request
import urllib.error
import json
import time

url = "https://text.pollinations.ai/"
data = json.dumps({"messages": [{"role": "user", "content": "merhaba"}], "model": "openai"}).encode("utf-8")
headers = {"Content-Type": "application/json"}

for i in range(5):
    print(f"Attempt {i}")
    req = urllib.request.Request(url, data=data, headers=headers, method="POST")
    try:
        response = urllib.request.urlopen(req)
        print("Status:", response.status)
        print("Content:", response.read().decode())
    except urllib.error.HTTPError as e:
        print("Status:", e.code)
        print("Content:", e.read().decode())
    time.sleep(3)
