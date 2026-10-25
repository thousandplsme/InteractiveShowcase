"""Send metadata only, exclusively to loopback."""
import json
import socket


class UdpSender:
    def __init__(self, port=5005):
        self._destination = ("127.0.0.1", port)
        self._socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self._socket.setblocking(False)

    def send(self, packet):
        payload = json.dumps(packet, allow_nan=False, separators=(",", ":")).encode("utf-8")
        try:
            self._socket.sendto(payload, self._destination)
            return True
        except OSError:
            return False  # UDP loss is recovered by the next frame / Unity timeout.

    def close(self):
        self._socket.close()
