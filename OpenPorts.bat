netsh http add urlacl url=http://[computername]:50594/ user=everyone
netsh http add urlacl url=http://[computername]:44330/ user=everyone
netsh http add urlacl url=https://[computername]:44330/ user=everyone

netsh http delete urlacl url=http://[computername]:8080/
netsh http delete urlacl url=http://[computername]:50594/
netsh http delete urlacl url=https://[computername]:44330/

netsh advfirewall firewall add rule name="IISExpressWeb" dir=in protocol=tcp localport=50594 profile=private remoteip=localsubnet action=allow
netsh advfirewall firewall add rule name="IISExpressWeb" dir=in protocol=tcp localport=8800 profile=private remoteip=localsubnet action=allow
netsh advfirewall firewall add rule name="IISExpressWeb" dir=in protocol=tcp localport=44330 profile=private remoteip=localsubnet action=allow

netsh advfirewall firewall add rule name="IISExpressWeb"