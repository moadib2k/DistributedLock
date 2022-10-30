# Globabl distributed lock library for .net
The intent of this library is to create an easy to use global lock library. This library is useful When you have entities that need to have access synchronized across microservces. 

## Dynamo Database
The first iteration of this library is using an AWS DyanmoDb table for the lock. Using the cdk file in the in deployment folder will create the table in an AWS account. 

## Sample 
There project MRB.DistributedLock.Sample is a small sample that shows how to inject and use the lock in simple use cases.
