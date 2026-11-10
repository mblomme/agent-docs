---
name: install-design-tests
description: Install and configure the repository to make use of the design tests suite.
---

# Install Design Tests

This skill installs a testing suite called "design tests" in the repository.
Detect where the test folder is located and create the design test project in the correct location.
The suite is provided in the `references` folder of this skill.

## Reference

A whole working testing suite is provided in the `references` folder. It contains a project with a set of design tests.
Refactor it so the namespaces and project names match the target repository. 

Do not add, remove or change the behavior of the tests. The tests are correct and should be kept as they are.

The aggregate value-object rule (`DDD_TA_P002`) requires each aggregate's
namespace-relative `ValueObjects` folder to contain 4 to 100 distinct concrete
value-object declarations, inclusive. Only declarations directly in that folder
count; identity types beside the aggregate and declarations in subfolders or
other aggregates do not.
