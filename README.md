# CQRS and Event Sourcing with kafka

The project is for learning how to create microservices in C# that are based on CQRS and Event Sourcing.
The virtual network has been set for databases and kafka including zookeeper in docker containers which is extremely useful in future deployments into the cloud infrastructure.

## Creating containers
First, we need to create a network where our resources will communicate.
```
docker network create --attachable -d bridge mydockernetwork
```
`attachable` means that we can attach our resources later

Running the command
```
docker-compose up -d 
```
creates a kafka stream on a port `9092` with zookeper service on the port `2181`.

Then create container for mongo-db:
```
docker run -it -d --name mongo-container -p 27017:27017 --network mydockernetwork --restart always -v mongodb_data_container:/data/db mongo:latest
```
In order to connect to a new mongo db, install one of the clients for it. 

The second container would be for MSSQL:
```
docker run -d --name sql-container --network mydockernetwork --restart always -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=P@$$w0rd1' -e 'MSSQL_PID=Express' -p 1438:1433 mcr.microsoft.com/mssql/server:2017-latest-ubuntu
```
We swap port `1433` to `1438` outside the container because the standard port `1433` for MS SQL is occupied by installed MS SQL on the machine. You can change the value to any free port on your machine.
