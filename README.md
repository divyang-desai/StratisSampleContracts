# Stratis Sample Contracts

## Overview

The voting contract application expresses a workflow of the voting process using a smart contract. It enables to provide correct voting rights, automatic vote counting, and process transparent.

## User Roles

1. **Chairperson:** 
Chairperson is the contract owner, who deploy the contract and provide the voting right to the voters.

2. **Voter:** The person who votes against the proposals.

## Methods

### Give Right To Vote

This method provides the voting rights to the voter to vote on this ballot. Only the chairperson can call this method.

### Vote
This method allows the voter to give the vote to a certain proposal.

### Winning Proposal
Computes the winning proposal taking all previous votes into account. This method returns the winning proposal index.

### Winner Name
Gets the name of the winning proposal by using the method `WinningProposal`



