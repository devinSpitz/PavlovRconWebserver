 #!/bin/bash
function slowcat(){ while read; do sleep .05; echo "$REPLY"; done; }
cat  $1 | slowcat | nc localhost {port}