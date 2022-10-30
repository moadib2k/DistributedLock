#!/usr/bin/env python3
import os

import aws_cdk as cdk

from deployment.deployment_stack import LockStack


app = cdk.App()
LockStack(app, "DistributedLockStack",)

app.synth()
