The goal was to design and implement a scalable,
high-throughput microservice in C#/.NET that ingests data from custom TCP streams,
handles message deduplication, and processes the data efficiently.

A. TCP Stream Ingestion:
    * Implement a server that listens on a specified TCP port.
    * The server must be able to handle multiple concurrent client connections efficiently.
    * The service should robustly handle continuous streams of binary data, including partial messages and stream synchronization.

B. Binary Protocol Parsing:
    The service receives data in a custom binary protocol format. Each message structure is as follows

    The service will receive messages over a continuous TCP stream. Each message follows this structure:

    [Sync Word] - [Device Id] - [Message Counter] - [Message Type] - [Payload Length] - [Payload]

    Sync Word - two bytes long, const 0xAA55.
    Device Id - a 4 byte long, uniq device identifier.
    Message Counter - a 2 byte long, should be incremented on every message
    Message Type - a byte marking the message type
    Payload Length - two bytes long, the rest of the message length in bytes.
    Payload - a message specific payload

    All multi-byte numeric fields (Sync Word, Payload Length, Device Id, Message Counter) are transmitted in Big Endian format

C. Message Deduplication and Validation:
    * Deduplication: If a Message Counter for a specific Device Id is received more than once, the subsequent messages with the same counter are considered duplicates and must be ignored.
    * Validation: Verify the Sync Word (0xAA55) and ensure the Payload Length matches the received data size. Invalid messages should be logged and discarded.

D. Data Processing and Routing:
    After successful ingestion, validation, and deduplication, the messages must be processed and routed based on the Message Type:
    * Device Messages (Types: 2, 11, 13): The Payload must be converted into a standardized JSON format representation of a "Device Message" and routed to a Device Message Queue.
    * Device Events (Types: 1, 3, 12, 14): The Payload should be routed directly to a Device Event Queue.

the solution should demonstrate a clean, well-structured, and easy to understand.


