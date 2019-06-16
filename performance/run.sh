#!/bin/bash
./wrk -t10 -c100 -d20 -s batch.lua http://192.168.128.221:5123/api/batch
