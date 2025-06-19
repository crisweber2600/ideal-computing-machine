Feature: Multi Drive Coordinator
  In order to process multiple root pairs
  As a developer
  I want the coordinator worker to aggregate counts for Google and Microsoft drives.

  Scenario: aggregating counts
    Given two root pairs exist
    When the coordinator processes the queue
    Then counts for all roots should be recorded
