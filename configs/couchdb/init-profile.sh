#!/bin/bash

echo "_lock DB created"
curl -X PUT -u admin:admin $HOST/lock

echo "Sending in test profile file..."
curl -d @/profiles/test-profile.json -X PUT -H "Content-Type: application/json" -u admin:admin $HOST/lock/test