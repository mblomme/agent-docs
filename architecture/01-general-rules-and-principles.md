# General rules and principles

The following rules and principles apply to all layers of the architecture. They are not specific to any layer.

## Premature optimization

Avoid premature optimization. Optimize only when there is a clear performance problem, and only after measuring and analyzing the system. The measuring and analyzing should be explicitly documented or requested by the product owner. Do not optimize based on assumptions or guesses.

## Premature abstraction

Avoid premature abstraction. Abstract only when there is a clear need for it. Never created abstraction for the sake of abstractions.

## Inside-out design

The inside should be designed and implemented first, technical details and concerns should be addressed in the infrastructure layer as much as possible. The inside layers are product and ubuiquitous language first and easy to understand by developers or product owners. Implementing the inside first helps to avoid technical debt and complexity and focuses on the core domain and business value.

