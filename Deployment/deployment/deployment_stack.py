from aws_cdk import (
    Duration,
    RemovalPolicy,
    Stack,
    aws_dynamodb as dynamodb,
    Aws
)


from constructs import Construct

class LockStack(Stack):
    def __init__(self, scope: Construct, construct_id: str, **kwargs) -> None:
        super().__init__(scope, construct_id, **kwargs)
        name = f'distributed-lock'
        table = dynamodb.Table(self, name,
            table_name=name,
            billing_mode=dynamodb.BillingMode.PAY_PER_REQUEST,
            removal_policy=RemovalPolicy.DESTROY,
            partition_key=dynamodb.Attribute(name='InstanceId', type=dynamodb.AttributeType.STRING),
            sort_key=dynamodb.Attribute(name='InstanceType', type=dynamodb.AttributeType.STRING),
            time_to_live_attribute='ttl')