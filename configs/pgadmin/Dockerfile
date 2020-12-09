FROM dpage/pgadmin4:4.20

COPY servers.json /pgadmin4/servers.json

CMD /usr/local/bin/python/python /pgadmin4/setup.py --load-servers servers.json --user admin