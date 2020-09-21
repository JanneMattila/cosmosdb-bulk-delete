# Notes

Perf testing is always tricky. Here are some
numbers with *very* **very** limited testing.
Your mileage *will* vary.

## Setup

This test setup has been done with Cosmos DB account and
Virtual Machine hosted in same Azure Region.

VM has been provisioned with following specs:

- Size: Standard D4s_v4 (4 vcpus, 16 GiB memory)
- Accelerated Networking: Enabled
- Connection from VM to Cosmos DB is via Service endpoint

For latency check you can use `psping` ([download](https://docs.microsoft.com/en-us/sysinternals/downloads/psping)):

```bash
# psping -t yourcosmosdbaccount.documents.azure.com:443

PsPing v2.10 - PsPing - ping, latency, bandwidth measurement utility
Copyright (C) 2012-2016 Mark Russinovich
Sysinternals - www.sysinternals.com

TCP connect to 13.69.126.21:443:
Infinite iterations (warmup 1) ping test:
Connecting to 13.69.126.21:443 (warmup): from 172.16.2.4:50762: 1.37ms
Connecting to 13.69.126.21:443: from 172.16.2.4:50763: 1.21ms
Connecting to 13.69.126.21:443: from 172.16.2.4:50764: 1.26ms
Connecting to 13.69.126.21:443: from 172.16.2.4:50765: 1.40ms
Connecting to 13.69.126.21:443: from 172.16.2.4:50766: 1.46ms
Connecting to 13.69.126.21:443: from 172.16.2.4:50767: 1.37ms
Connecting to 13.69.126.21:443: from 172.16.2.4:50768: 1.28ms
Connecting to 13.69.126.21:443: from 172.16.2.4:50769: 1.21ms
Connecting to 13.69.126.21:443: from 172.16.2.4:50771: 1.33ms
Connecting to 13.69.126.21:443: from 172.16.2.4:50772: 1.32ms
Connecting to 13.69.126.21:443: from 172.16.2.4:50773: 1.28ms

  Sent = 10, Received = 10, Lost = 0 (0% loss),
  Minimum = 1.21ms, Maximum = 1.46ms, Average = 1.31ms
```

Test scenario:

- Write 100'000 documents
- Delete 100'000 documents
- Times in seconds

| RUs    | Write | Delete | Documents/second |
|--------|-------|--------|------------------|
| 5'000  |       |        |                  |
| 10'000 |       |        |                  |
| 20'000 |       |        |                  |
| 30'000 |       |        |                  |
| 50'000 |       |        |                  |
