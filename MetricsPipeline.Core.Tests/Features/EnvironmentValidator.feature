Feature: Environment Validator
  In order to catch misconfigured credentials
  As an operator
  I want validation errors before scanning begins.

  Scenario: validation succeeds with correct configuration
    Given all required environment variables are set
    When I validate the environment
    Then validation should succeed

  Scenario: missing credentials file
    Given all required environment variables are set
    And GOOGLE_AUTH is set to a nonexistent path
    When I validate the environment
    Then validation should fail with message containing "Google credentials not found"

  Scenario: missing variables
    Given environment variables are cleared
    When I validate the environment
    Then validation should fail with message containing "MS_ROOT"
