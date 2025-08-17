## TBFT

### Algorithm: Initialization

```pseudo
Function InitConsensus()
    nodes ← new ConsensusNodes() 
        // Initialize container for all consensus nodes
    For i ← 0 to N-1
        node ← new ConsensusNode(1, i, 0, 0, N, 0, 0)
            // Create a node with initial parameters
        nodes.addConsensusNode(node)
            // Add node to the global node list
        nodeTimeout[i] ← consensusTimeout
            // Set initial timeout for each node
    EndFor
    → Skip deadline[0]
        // Trigger timeout handling for the first round
EndFunction


Function StartConsensus(nodeId, gb)
    ProcessBlock(nodeId, nodes.getByKey(nodeId), gb)
        // Begin consensus using the genesis block
    ∥ StartListener(nodeId)
        // Start network listener for message handling
EndFunction


Function ProcessBlock(nodeId, node, block)
    node.processBlock(block)
        // Store or validate incoming block at the node
    → Skip deadline[0]
        // Trigger timeout for this block height
    NewHeight(nodeId, node, block.getHeight() + 1)
        // Advance to next block height in consensus
EndFunction
```

### Algorithm: New Height

```pseudo
Function NewHeight(nodeId, node, height)
    node.newHeight(height)
    node.setStep(Step_NEW_HEIGHT)
    → Skip deadline[0]
    NewRound(nodeId, node, height, 0)
EndFunction
```

### Algorithm: New Round

```pseudo
Function NewRound(nodeId, node, height, round)
    node.setStep(Step_NEW_ROUND)
    node.newRound(height, round)
    nodeTimeout[nodeId] ← consensusTimeout + round
    → Skip deadline[0]
    MockCorrupt(nodeId, node, height, round)
EndFunction
```

### Algorithm: Mock Corrupt

```pseudo
Function MockCorrupt(nodeId, node, height, round)
    Corrupt(nodeId, height, round, node.getLastProposer())
        // Send delay instructions to target nodes
    ∥ Corrupted(nodeId, node, height, round)
        // Simulate receiving delay info
    deadline[0]
    ProposeBlock(nodeId, node, height, round, node.getLastProposer())
EndFunction


Function Corrupt(nodeId, h, r, lp)
    Switch attackCase
        Case CorruptProposerLowerTimeout
            SendCorruptToProposer(nodeId, corruptLowerTimeout, h, r, lp)
        Case CorruptProposerAndValidatorLowerTimeout
            SendCorruptToProposerAndValidator(nodeId, corruptLowerTimeout, h, r, lp)
        Case CorruptValidatorLowerTimeout
            SendCorruptToValidator(nodeId, corruptLowerTimeout, h, r, lp)
        Case CorruptProposerHeigherTimeout
            SendCorruptToProposer(nodeId, corruptHigherTimeout, h, r, lp)
        Case CorruptProposerAndValidatorHigherTimeout
            SendCorruptToProposerAndValidator(nodeId, corruptHigherTimeout, h, r, lp)
        Case CorruptValidatorHigherTimeout
            SendCorruptToValidator(nodeId, corruptHigherTimeout, h, r, lp)
        Default
            SendCorruptToNone(nodeId)
    EndSwitch
EndFunction


Function SendCorruptToNone(nodeId)
    corruptChannel[nodeId]!defaultConnTime → Skip
        // No delay; default communication time = 1 unit
EndFunction


Function SendCorruptToProposer(nodeId, delay, h, r, lp)
    If nodeId = call(NextProposer, lp, h, r, N)
        corruptChannel[nodeId]!delay → Skip
    Else
        corruptChannel[nodeId]!defaultConnTime → Skip
    EndIf
        // Delay only for current proposer
EndFunction


Function SendCorruptToProposerAndValidator(nodeId, delay, h, r, lp)
    If nodeId = call(NextProposer, lp, h, r, N) ∨ nodeId = (call(NextProposer, lp, h, r, N) + 1) mod N
        corruptChannel[nodeId]!delay → Skip
    Else
        corruptChannel[nodeId]!defaultConnTime → Skip
    EndIf
        // Delay for proposer and one validator
EndFunction


Function SendCorruptToValidator(nodeId, delay, h, r, lp)
    If nodeId = (call(NextProposer, lp, h, r, N) + 1) mod N
        corruptChannel[nodeId]!delay → Skip
    Else
        corruptChannel[nodeId]!defaultConnTime → Skip
    EndIf
        // Delay only for one validator
EndFunction


Function Corrupted(nodeId, node, height, round)
    corruptChannel[nodeId]?delay → saveDelayTime
    node.setDelayTime(height, round, delay)
    → Skip
        // Node records the received delay time
EndFunction
```

### Algorithm: Propose

```pseudo
Function ProposeBlock(nodeId, node, height, round, lastProposer)
    node.setStep(Step_PROPOSE)
        // Set node's consensus step to PROPOSE
    If node.needGenProposal(height, round, (attackCase = DisguiseProposer ∨ DisguiseProposer2)) = 1
        GenerateProposal(node, nodeId, height, round)
            // Only proposer or attacker generates the proposal
    Else
        Skip
    EndIf
    deadline[0]
    atomic
    If attackCase = DisguiseProposer ∧ node.isDisguiser(height, round) = 1
        ( WaitProposal(nodeId) ∥
          SendMessage(nodeId, proposals.getProposal(nodeId, height, round),
                      defaultConnTime, (nodeId+1) mod N) ∥
          Prevote(nodeId, node, height, round) )
            // Simulate behavior of a disguised malicious proposer
    Else
        ( WaitProposal(nodeId) ∥
          BroadcastMessage(nodeId, proposals.getProposal(nodeId, height, round),
                           node.getDelayTime(height, round)) ∥
          Prevote(nodeId, node, height, round) )
            // Normal proposer broadcasts proposal and enters prevote
    EndIf
EndFunction


Function GenerateProposal(node, nodeId, height, round)
    proposal ← node.generateProposal()
        // Node generates a block proposal
    proposals.addProposal(nodeId, height, round, proposal)
        // Proposal is stored for later access
    → Skip
EndFunction


Function WaitProposal(nodeId)
    atomic
    receivedProposalCh[nodeId]?ps → procProposal
        // Wait for proposal reception via channel
    do
        a ← 0
        → enterPrevoteCh[nodeId]!1 → Skip
        timeout[nodeTimeout[nodeId]] → enterPrevoteCh[nodeId]!0 → Skip within[0]
            // Enter prevote step on message or timeout
EndFunction
```

### Algorithm: Prevote

```pseudo
Function Prevote(nodeId, node, height, round)
    atomic
    enterPrevoteCh[nodeId]?f → prevote
        // Receive signal to enter prevote step
    do
        node.setStep(Step_PREVOTE)
            // Update node's consensus step
        prevote ← node.generatePreVote(f)
            // Generate the prevote message based on proposal or timeout
        prevotes.addVote(nodeId, height, round, prevote)
            // Record the prevote for this node at given height and round
        node.setStep(Step_PREVOTE_WAIT)
        → Skip
        ( WaitPrevote(nodeId, node) ∥
          BroadcastMessage(nodeId, prevotes.getByKey(nodeId, height, round),
                           node.getDelayTime(height, round)) ∥
          Precommit(nodeId, node, height, round) )
            // Wait for votes, broadcast own vote, and proceed to precommit
EndFunction


Function WaitPrevote(nodeId, node)
    atomic
    majorityPrevoteCh[nodeId]?pv →
        // Wait for enough prevotes
    node.setStep(Step_PRECOMMIT)
    → enterPrecommitCh[nodeId]!1 → Skip
    timeout[nodeTimeout[nodeId]] → enterPrecommitCh[nodeId]!0 → Skip within[0]
        // Proceed to precommit on message or timeout
EndFunction
```

### Algorithm: Precommit

```pseudo
Function Precommit(nodeId, node, height, round)
    atomic
    enterPrecommitCh[nodeId]?f →
        // Receive signal to enter precommit step 
    do
        precommit ← node.generatePreCommit(f)
            // Generate a precommit vote
        precommits.addVote(nodeId, height, round, precommit)
            // Store the precommit vote in the vote set
        node.setStep(Step_PRECOMMIT_WAIT)
        → Skip
        ( WaitPrecommit(nodeId, node) ∥
          BroadcastMessage(nodeId, precommits.getByKey(nodeId, height, round),
                           node.getDelayTime(height, round)) ∥
          Commit(nodeId, node, height, round) )
            // Wait for precommits, send own, and prepare to commit
EndFunction


Function WaitPrecommit(nodeId, node)
    atomic
    majorityPrecommitCh[nodeId]?pc → waitPrecommit
        // Wait until a majority of precommit votes are received
    do
        node.setStep(Step_COMMIT)
            // Move to commit step upon receiving enough precommits
        → enterCommitCh[nodeId]!1 → Skip
        timeout[nodeTimeout[nodeId]] → enterCommitCh[nodeId]!0 → Skip within[0]
            // Fallback: proceed on timeout if majority not reached
EndFunction
```

### Algorithm: Commit

```pseudo
Function Commit(nodeId, node, height, round)
    enterCommitCh[nodeId]?f →
        // Wait for commit trigger signal; f=1 indicates quorum precommit received
    If f = 1 ∧ node.isNilConsensus(height, round, Step_COMMIT) ≠ 1
        CommitBlock(nodeId, node)
            // Proceed to commit the block if not a nil consensus
    Else
        NewRound(nodeId, node, height, round+1)
            // Initiate a new consensus round otherwise
    EndIf
EndFunction


Function CommitBlock(nodeId, node)
    commit
    node.commitBlock()
        // Finalize and append the proposed block to the blockchain
    →
    If node.reachTarget(TARGET) = 1
        BroadcastMessage(nodeId, -1, 0)
            // Broadcast termination message if the consensus target is reached
    Else
        ProcessBlock(nodeId, node, node.getProposalBlock())
            // Otherwise, proceed with processing the next block proposal
    EndIf
EndFunction