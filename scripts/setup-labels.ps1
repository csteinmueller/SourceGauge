#!/usr/bin/env pwsh
# Creates/updates the GitHub labels used by issue templates and the process. Requires GitHub CLI (gh) and maintainer rights.
$ErrorActionPreference = 'Stop'

$labels = @(
    @{ Name = 'triage';         Color = 'fbca04'; Description = 'Needs maintainer triage' }
    @{ Name = 'bug';            Color = 'd73a4a'; Description = 'Something is not working' }
    @{ Name = 'enhancement';    Color = 'a2eeef'; Description = 'New feature or metric' }
    @{ Name = 'requirement';    Color = '0e8a16'; Description = 'Functional or non-functional requirement' }
    @{ Name = 'tech-debt';      Color = 'b60205'; Description = 'Technical debt to be paid back' }
    @{ Name = 'adr';            Color = '5319e7'; Description = 'Architecture decision' }
    @{ Name = 'ai-assisted';    Color = 'c5def5'; Description = 'Change produced with AI assistance' }
    @{ Name = 'good first issue'; Color = '7057ff'; Description = 'Good for newcomers' }
)

foreach ($l in $labels) {
    gh label create $l.Name --color $l.Color --description $l.Description --force
}
