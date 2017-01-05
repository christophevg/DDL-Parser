TARGET     = main.exe
BUILD      = .build
ifdef DEBUG
CFLAGS   += -define:DEBUG
endif
CC         = mcs
RUN        = mono
ARGUMENTS ?=

all: run

$(BUILD)/%.exe: src/%.cs
	@mkdir -p $(BUILD)
	@$(CC) $(CFLAGS) -out:$@ $<

run: $(BUILD)/$(TARGET)
	@$(RUN) $< $(ARGUMENTS)
